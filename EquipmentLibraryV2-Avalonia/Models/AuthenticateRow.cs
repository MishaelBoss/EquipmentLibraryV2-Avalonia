namespace EquipmentLibraryV2_Avalonia.Models;

public sealed class AuthenticateRow
{
    public string Status { get; set; } = string.Empty;
    public int? RetryAfterSeconds { get; set; }
    public int? UserId { get; set; }
    public string? UserLogin { get; set; }
    public int? UserRole { get; set; }
}