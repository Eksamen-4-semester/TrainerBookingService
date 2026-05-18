using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using NLog;
using NLog.Web;
using TrainerBookingService.Repositories;
using TrainerBookingService.Repositories.Interfaces;
using VaultSharp;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;    

Console.WriteLine("Starting TrainerBookingService");
Console.WriteLine($"VAULT_URL: {Environment.GetEnvironmentVariable("VAULT_URL")}");

var logger = LogManager.Setup().LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

logger.Debug("Starting TrainerBookingService");

var endPoint = Environment.GetEnvironmentVariable("VAULT_URL") ?? "https://localhost:8201/";
logger.Debug("Connecting to Hashicorp Vault on: {0}", endPoint);

var httpClientHandler = new HttpClientHandler();
httpClientHandler.ServerCertificateCustomValidationCallback =
    (message, cert, chain, sslPolicyErrors) => { return true; };

IAuthMethodInfo authMethod =
    new TokenAuthMethodInfo("00000000-0000-0000-0000-000000000000");

var vaultClientSettings = new VaultClientSettings(endPoint, authMethod)
{
    Namespace = "",
    MyHttpClientProviderFunc = handler
        => new HttpClient(httpClientHandler) {
            BaseAddress = new Uri(endPoint)
        }
};

logger.Debug("Getting JWT secret, DB connectionstring and database name from Vault");
IVaultClient vaultClient = new VaultClient(vaultClientSettings);

try
{
    Secret<SecretData> jwtSecret = await vaultClient.V1.Secrets.KeyValue.V2
        .ReadSecretAsync(path: "auth", mountPoint: "secret");
    string jwtSecretString = jwtSecret.Data.Data["JWT_SECRET"].ToString();
    if (string.IsNullOrWhiteSpace(jwtSecretString))
        throw new NullReferenceException("JWT_SECRET not found");
    Environment.SetEnvironmentVariable("JWT_SECRET", jwtSecretString);

    Secret<SecretData> mongoSecrets = await vaultClient.V1.Secrets.KeyValue.V2
        .ReadSecretAsync(path: "mongo", mountPoint: "secret");
    
    string connectionString;
    if (Environment.GetEnvironmentVariable("DOCKER") != null)
    {
        connectionString = mongoSecrets
                               .Data.Data["MONGO_CONNECTION_STRING"]?.ToString()
                           ?? throw new NullReferenceException(
                               "MONGO_CONNECTION_STRING not found in Vault");
    }
    else
    {
        connectionString = "mongodb://admin:secret123@localhost:27017/?authSource=admin";
    }
    Environment.SetEnvironmentVariable("MONGO_CONNECTION_STRING", connectionString);

    string mongoDbName = mongoSecrets.Data.Data["MONGO_TRAINERBOOKING_DB"].ToString();
    if (string.IsNullOrWhiteSpace(mongoDbName))
        throw new NullReferenceException("MONGO_DATABASE_NAME not found");
    Environment.SetEnvironmentVariable("MONGO_DATABASE_NAME", mongoDbName);
}
catch (Exception e)
{
    Console.WriteLine("Vault error: " + e.Message);
    Console.WriteLine("Vault inner error: " + e.InnerException?.Message);
}

try
{
    var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

    builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();
    
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(o =>
        {
            o.RequireHttpsMetadata = false;
            o.TokenValidationParameters = new TokenValidationParameters()
            {
                IssuerSigningKey =
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET"))),
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ClockSkew = TimeSpan.Zero,
            };
        });
    builder.Services.AddAuthorization();

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();
    
    builder.Services.AddSingleton<IMongoClient>(sp =>
    {
        var connectionString = Environment.GetEnvironmentVariable("MONGO_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("MONGO_CONNECTION_STRING environment variable is not set");
        return new MongoClient(connectionString);
    });

    builder.Services.AddScoped<IMongoDatabase>(sp =>
    {
        var mongoClient = sp.GetRequiredService<IMongoClient>();
        var databaseName = Environment.GetEnvironmentVariable("MONGO_DATABASE_NAME");
        if (string.IsNullOrWhiteSpace(databaseName))
            throw new InvalidOperationException("MONGO_DATABASE_NAME environment variable is not set");
        return mongoClient.GetDatabase(databaseName);
    });

    builder.Services.AddScoped<ITrainerBookingRepository, TrainerBookingRepository>();

    builder.Services.AddHttpClient("userService", client =>
    {
        var userServiceUrl = Environment.GetEnvironmentVariable("USERSERVICE_URL");
        if (string.IsNullOrWhiteSpace(userServiceUrl))
            logger.Warn("USERSERVICE_URL is not set, using localhost instead");
        client.BaseAddress = new Uri(userServiceUrl ?? "http://localhost:5001/");
    });

    builder.Services.AddHttpClient("membershipService", client =>
    {
        var membershipServiceUrl = Environment.GetEnvironmentVariable("MEMBERSHIPSERVICE_URL");
        if (string.IsNullOrWhiteSpace(membershipServiceUrl))
            logger.Warn("MEMBERSHIPSERVICE_URL is not set, using localhost instead");
        client.BaseAddress = new Uri(membershipServiceUrl ?? "http://localhost:5002/");
    });

    var app = builder.Build();

// Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();
    
    app.UseAuthentication();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}

catch (Exception ex)
{
    logger.Error(ex, "Stopped program because of exception");
    throw;
}

finally
{
    NLog.LogManager.Shutdown();
}

