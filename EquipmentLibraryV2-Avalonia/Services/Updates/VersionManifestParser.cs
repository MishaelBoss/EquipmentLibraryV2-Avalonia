using System.Text.Json;
using EquipmentLibraryV2_Avalonia.Models;

namespace EquipmentLibraryV2_Avalonia.Services.Updates;

internal static class VersionManifestParser
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive =  true
    };
    
    public static VersionManifest? Parse(string jsonString) 
        =>  JsonSerializer.Deserialize<VersionManifest>(jsonString, Options);
}