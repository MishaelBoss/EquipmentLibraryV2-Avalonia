using System;

namespace EquipmentLibraryV2_Avalonia.Helpers;

public static class VersionHelper
{
    public static bool IsNewerVersion(string current, string latest)
    {
        return Version.TryParse(current, out var currentVersion)
                   && Version.TryParse(latest, out var latestVersion)
                   && latestVersion > currentVersion;
    }
}
