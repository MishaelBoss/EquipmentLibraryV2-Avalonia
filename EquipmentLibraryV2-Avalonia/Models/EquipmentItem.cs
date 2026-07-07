using System;

namespace EquipmentLibraryV2_Avalonia.Models;

public class EquipmentItem
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? InvNum { get; set; }
    public int? EquipmentTypeId { get; set; }
    public string? EquipmentTypeName { get; set; }
    public long? UserId { get; set; }
    public string? UserLogin { get; set; }
    public DateTime? DateAdded { get; set; }
    public bool IsActive { get; set; } = true;
}
