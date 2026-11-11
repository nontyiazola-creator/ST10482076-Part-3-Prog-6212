namespace RaceDay.API.Models;

public class Result
{
    public int ResultID { get; set; }
    public int EnrolmentID { get; set; }
    public Enrolment Enrolment { get; set; } = null!;
    public TimeSpan? FinishTime { get; set; }
    public int? FinishPosition { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.Now;
}