using EquipmentLibraryV2_Avalonia.Services.Interfaces;

namespace EquipmentLibraryV2_Avalonia.Modelsl;

public class RefreshTokenData(string refreshToken, DateTime expires) : IRefreshTokenData
{
    public string RefreshToken { get; set; } = refreshToken;
    public DateTime Expires { get; set; } = expires;
}
