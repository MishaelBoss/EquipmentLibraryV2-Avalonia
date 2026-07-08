using EquipmentLibraryV2_Avalonia.Services.Interfaces;

namespace EquipmentLibraryV2_Avalonia.Models;

public class PackageInfo (string name, string version) : IPackageInfo
{
    public string PackageName { get; set; } = name;
    public string PackageVersion { get; set; } = version;
}
