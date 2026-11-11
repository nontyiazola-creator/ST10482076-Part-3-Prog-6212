namespace RaceDay.API.Models;

public class Role
{
    public int RoleID { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public ICollection<User> Users { get; set; } = new List<User>();
}