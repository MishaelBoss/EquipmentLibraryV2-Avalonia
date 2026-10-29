using EquipmentLibraryV2_Avalonia.Services.Interfaces;

namespace EquipmentLibraryV2_Avalonia.Models;

public class User : IUser
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public int UserRole { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string MiddleName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime DateJoined { get; set; }
    
    public User(int id, string login, int userRole)
    {
        Id = id;
        Login = login;
        UserRole = userRole;
    }
    
    public User()
    {
    }
}