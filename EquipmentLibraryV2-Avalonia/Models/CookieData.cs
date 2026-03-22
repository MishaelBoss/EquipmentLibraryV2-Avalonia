using EquipmentLibraryV2_Avalonia.Services.Interfaces;
using System;

namespace EquipmentLibraryV2_Avalonia.Models
{
    public class CookieData(double id, string login, string token, DateTime expires) : ICookieData
    {
        public double Id { get; set; } = id;
        public string Login { get; set; } = login;
        public string Token { get; set; } = token;
        public DateTime Expires { get; set; } = expires;
    }
}
