namespace RaceDay.API.Models;

public class Enrolment
{
    public int EnrolmentID { get; set; }
    public int ParticipantID { get; set; }
    public User Participant { get; set; } = null!;
    public int EventID { get; set; }
    public Event Event { get; set; } = null!;
    public int CategoryID { get; set; }
    public Category Category { get; set; } = null!;
    public DateTime EnrolmentDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = "Confirmed";
    public Result? Result { get; set; }
}