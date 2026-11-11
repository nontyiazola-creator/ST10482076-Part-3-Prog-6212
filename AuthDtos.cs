namespace RaceDay.API.Dtos;

public class RegisterDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    /// <summary>Organiser or Participant</summary>
    public string Role { get; set; } = string.Empty;
}

public class LoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class EventDto
{
    public string EventName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime EventDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public decimal Distance { get; set; }
    /// <summary>Run, Walk or Cycle</summary>
    public string EventType { get; set; } = "Run";
}

public class CategoryDto
{
    /// <summary>e.g. Under 20, Senior, 10km, 21km</summary>
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class EnrolmentDto
{
    public int EventID { get; set; }
    public int CategoryID { get; set; }
}

public class ResultDto
{
    /// <summary>Format hh:mm:ss, e.g. 00:45:30</summary>
    public TimeSpan? FinishTime { get; set; }
    public int? FinishPosition { get; set; }
}

public class UpdateProfileDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}