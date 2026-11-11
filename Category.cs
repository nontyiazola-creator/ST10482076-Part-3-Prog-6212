namespace RaceDay.API.Models;

public class Category
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public Event Event { get; set; } = null!;
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}