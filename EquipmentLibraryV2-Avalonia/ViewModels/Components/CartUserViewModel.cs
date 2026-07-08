using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dapper;
using EquipmentLibraryV2_Avalonia.Messages;
using Npgsql;
using Serilog;
using System;
using System.Threading.Tasks;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Services;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components
{
    public partial class CartUserViewModel : ViewModelBase
    {
        public long UserId { get; init; }
        public int UserRole { get; init; }
        [ObservableProperty] public partial string Login { get; set; } = string.Empty;
        [ObservableProperty] public partial string MiddleName { get; set; } = string.Empty;
        [ObservableProperty] public partial string Password { get; set; } = string.Empty;
        [ObservableProperty] public partial string FirstName { get; set; } = string.Empty;
        [ObservableProperty] public partial string LastName { get; set; } = string.Empty;
        [ObservableProperty] public partial string DateJoined { get; set; } = string.Empty;
        [ObservableProperty] public partial bool? IsActive { get; set; }

        [RelayCommand]
        public void CopyPassword()
        {
            TextCopy.ClipboardService.SetText(Password);
        }

        [RelayCommand]
        public void Edit()
        {
            WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditUserMessage(UserId, Login, FirstName, LastName, Password, UserRole));
        }

        [RelayCommand]
        public void Delete()
        {
            WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmDeleteMessage(UserId, Login, "DELETE FROM public.users WHERE id = @id", () => WeakReferenceMessenger.Default.Send(new RefreshUserListMessage())));
        }

        [RelayCommand]
        private async Task ChangeActiveUserAsync()
        {
            try
            {
                await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
                const string sql = "UPDATE public.users SET is_active = @is_active WHERE id = @id";
                await connection.ExecuteScalarAsync(sql, new { id = UserId, is_active = IsActive ?? true });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to toggle user active state for UserId={UserId}", UserId);
            }
        }
    }
}
