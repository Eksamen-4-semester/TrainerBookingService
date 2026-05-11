namespace Models;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

public class TrainerBooking
{
    [BsonId]
    public int BookingId { get; set; }
    public int MemberId { get; set; }
    public int TrainerId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    
    public TrainerBooking(int memberId, int trainerId, DateTime startTime, DateTime endTime)
    {
        MemberId = memberId;
        TrainerId = trainerId;
        StartTime = startTime;
        EndTime = endTime;
    }
}
