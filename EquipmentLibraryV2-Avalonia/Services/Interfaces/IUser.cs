namespace EquipmentLibraryV2_Avalonia.Services.Interfaces;

internal interface IUser
{
    public int Id { get; set; }
    public string Login { get; set; }
    public int UserRole { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string MiddleName { get; set; }
    public bool IsActive { get; set; }
    public DateTime DateJoined { get; set; }
}
