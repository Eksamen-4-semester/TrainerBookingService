using Models;

namespace TrainerBookingService.Repositories.Interfaces;

public interface ITrainerBookingRepository
{
    Task<TrainerBooking?> CreateBooking(int memberId, int trainerId, DateTime startTime, DateTime endTime);
    Task<bool> CancelBooking(int bookingId);
    Task<IEnumerable<TrainerBooking>> GetAllBookings();
    Task<TrainerBooking> GetBooking(int bookingId);
    Task<IEnumerable<TrainerBooking>> GetBookingsByMember(int memberId);

}