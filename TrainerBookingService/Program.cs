using NLog;
using NLog.Web;
using MongoDB.Driver;
using TrainerBookingService.Controllers;
using TrainerBookingService.Repositories;
using TrainerBookingService.Repositories.Interfaces;


var logger = LogManager.Setup().LoadConfigurationFromAppSettings()
    .GetCurrentClassLogger();

logger.Debug("Starting TrainerBookingService");

try
{
    var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

    builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    builder.Logging.ClearProviders();
    builder.Host.UseNLog();
    
    var connectionString = builder.Configuration["MongoDB:ConnectionString"];
    var databaseName = builder.Configuration["MongoDB:DatabaseName"];

    builder.Services.AddSingleton<IMongoClient>(new MongoClient(connectionString));
    builder.Services.AddSingleton(sp => 
        sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName));
    builder.Services.AddScoped<ITrainerBookingRepository, TrainerBookingRepository>();

    var app = builder.Build();

// Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

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

