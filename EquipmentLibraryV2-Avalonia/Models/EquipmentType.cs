using EquipmentLibraryV2_Avalonia.Services.Interfaces;

namespace EquipmentLibraryV2_Avalonia.Models;

public class EquipmentType(int id, string type) : IEquipmentType
{
    public int Id { get; set; } = id;
    public string Type { get; set; } = type;

    public override string ToString()
    {
        return Type;
    }
}
