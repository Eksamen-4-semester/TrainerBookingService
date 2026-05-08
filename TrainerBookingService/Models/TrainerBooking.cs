namespace PersonalTrainerService.Models;

public class TrainerBooking
{
    public int BookingId { get; private set; }
    public int MemberId { get; private set; }
    public int TrainerId { get; private set; }
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    
    public TrainerBooking(int memberId, int trainerId, DateTime startTime, DateTime endTime)
    {
        MemberId = memberId;
        TrainerId = trainerId;
        StartTime = startTime;
        EndTime = endTime;
    }
}
