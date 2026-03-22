using EquipmentLibraryV2_Avalonia.Services.Interfaces;

namespace EquipmentLibraryV2_Avalonia.Models
{
    public class UserRole(int id, string type) : IUserRole
    {
        public int Id { get; set; } = id;
        public string Type { get; set; } = type;

        public override string ToString()
        {
            return Type;
        }
    }
}
