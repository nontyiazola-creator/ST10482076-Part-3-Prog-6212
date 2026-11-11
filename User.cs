using Microsoft.Extensions.Logging;

namespace RaceDay.API.Models;

public class User
{
    public int UserID { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public int RoleID { get; set; }
    public Role Role { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ICollection<Event> Events { get; set; } = new List<Event>();
    public ICollection<Enrolment> Enrolments { get; set; } = new List<Enrolment>();
}