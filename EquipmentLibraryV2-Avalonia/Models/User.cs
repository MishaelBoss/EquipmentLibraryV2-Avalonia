using EquipmentLibraryV2_Avalonia.Services.Interfaces;

namespace EquipmentLibraryV2_Avalonia.Models;

public class User(int id, string login, int userRole) : IUser
{
    public int Id { get; set; } = id;
    public string Login { get; set; } = login;
    public int UserRole { get; set; } = userRole;
}