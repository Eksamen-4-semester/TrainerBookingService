using MongoDB.Driver;
using Models;
using TrainerBookingService.Repositories.Interfaces;

namespace TrainerBookingService.Repositories;

public class TrainerBookingRepository : ITrainerBookingRepository
{
    private readonly ILogger<TrainerBookingRepository> _logger;
    private readonly IMongoCollection<TrainerBooking> _trainerBookingCollection;

    public TrainerBookingRepository(IMongoDatabase database,
        ILogger<TrainerBookingRepository> logger)
    {
        _logger = logger;
        _trainerBookingCollection = database.GetCollection<TrainerBooking>("TrainerBookings");
    }

    public async Task<TrainerBooking?> CreateBooking(int memberId, int trainerId, DateTime startTime, DateTime endTime)
    {
        _logger.LogDebug("CreateBooking called from {Repo}", nameof(TrainerBookingRepository));
        try
        {
            var booking = new TrainerBooking(memberId, trainerId, startTime, endTime);
            booking.BookingId = await GetMaxId() + 1;
            await _trainerBookingCollection.InsertOneAsync(booking);
            return booking;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CreateBooking failed");
            return null;
        }
    }

    public async Task<bool> CancelBooking(int bookingId)
    {
        _logger.LogDebug("CancelBooking called from {Repo}", nameof(TrainerBookingRepository));
        try
        {
            var filter = Builders<TrainerBooking>.Filter.Eq(x => x.BookingId, bookingId);
            var result = await _trainerBookingCollection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CancelBooking failed for bookingId {BookingId}", bookingId);
            return false;
        }
    }

    public async Task<IEnumerable<TrainerBooking>> GetAllBookings()
    {
        _logger.LogDebug("GetAllBookings called from {Repo}", nameof(TrainerBookingRepository));
        try
        {
            return await _trainerBookingCollection.Find(Builders<TrainerBooking>.Filter.Empty).ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetAllBookings failed");
            return Enumerable.Empty<TrainerBooking>();
        }
    }

    public async Task<TrainerBooking> GetBooking(int bookingId)
    {
        _logger.LogDebug("GetBooking called from {Repo}", nameof(TrainerBookingRepository));
        try
        {
            var filter = Builders<TrainerBooking>.Filter.Eq(x => x.BookingId, bookingId);
            return await _trainerBookingCollection.Find(filter).FirstOrDefaultAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetBooking failed for bookingId {BookingId}", bookingId);
            return null;
        }
    }
    
    private async Task<int> GetMaxId()
    {
        var sort = Builders<TrainerBooking>.Sort.Descending("_id");
        var result = await _trainerBookingCollection.Find(Builders<TrainerBooking>.Filter.Empty)
            .Sort(sort).Limit(1).FirstOrDefaultAsync();
        return result?.BookingId ?? 0;
    }
    
    public async Task<IEnumerable<TrainerBooking>> GetBookingsByMember(int memberId)
    {
        _logger.LogDebug("GetBookingsByMember called from {Repo}", nameof(TrainerBookingRepository));
        try
        {
            var filter = Builders<TrainerBooking>.Filter.Eq(x => x.MemberId, memberId);
            return await _trainerBookingCollection.Find(filter).ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetBookingsByMember failed for memberId {MemberId}", memberId);
            return Enumerable.Empty<TrainerBooking>();
        }
    }
}