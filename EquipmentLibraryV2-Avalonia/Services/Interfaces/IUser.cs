namespace EquipmentLibraryV2_Avalonia.Services.Interfaces;

internal interface IUser
{
    public int Id { get; set; }
    public string Login { get; set; }
    public int UserRole { get; set; }
}
