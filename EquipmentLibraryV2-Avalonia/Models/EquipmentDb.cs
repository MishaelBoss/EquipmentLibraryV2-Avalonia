namespace EquipmentLibraryV2_Avalonia.Models;

public class EquipmentDb
{
    public string Title { get; set; } = string.Empty;
    public long UserId { get; set; }
    public int EquipmentTypeId { get; set; }
    public string SerialNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string InvNum { get; set; } = string.Empty;
}