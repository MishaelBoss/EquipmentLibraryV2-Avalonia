namespace EquipmentLibraryV2_Avalonia.Services.Interfaces;

public interface IRefreshTokenData
{
    public string RefreshToken { get; set; }
    public DateTime Expires { get; set; }
}
