namespace EquipmentLibraryV2_Avalonia.Models;

public class VersionDetail
{
    public string Description { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
}

public class VersionManifest
{
    public Dictionary<string, string> Promos { get; set; } = [];
    public Dictionary<string, VersionDetail> Detail { get; set; } = [];
}