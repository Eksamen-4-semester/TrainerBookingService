namespace Models;

public class TrainerBookingDto
{
    public int TrainerId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}