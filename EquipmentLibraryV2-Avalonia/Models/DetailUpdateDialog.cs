namespace EquipmentLibraryV2_Avalonia.Models;

public class DetailUpdateDialog(string newVersion, string releaseNotes, string releaseDate)
{
    public string NewVersion { get; set; } = newVersion;
    public string ReleaseNotes { get; set; } = releaseNotes;
    public string ReleaseDate { get; set; } = releaseDate;
}