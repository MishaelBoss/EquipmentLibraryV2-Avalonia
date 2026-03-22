using System;

namespace EquipmentLibraryV2_Avalonia.Services.Interfaces
{
    public interface ICookieData
    {
        double Id { get; set; }
        string Login { get; set; }
        string Token { get; set; }
        DateTime Expires { get; set; }
    }
}
