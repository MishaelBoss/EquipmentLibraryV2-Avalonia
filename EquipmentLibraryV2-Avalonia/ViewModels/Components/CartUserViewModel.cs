using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dapper;
using EquipmentLibraryV2_Avalonia.Messages;
using Npgsql;
using Serilog;
using EquipmentLibraryV2_Avalonia.Infrastructure;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components
{
    public partial class CartUserViewModel : ViewModelBase
    {
        public long UserId { get; init; }
        public int UserRole { get; init; }
        [ObservableProperty] public partial string Login { get; set; } = string.Empty;

        partial void OnLoginChanged(string value)
        {
            Log.Debug("Login changed in CartUserViewModel. UserId={UserId}, NewLogin={Login}", UserId, value);
            OnPropertyChanged(nameof(AvatarChar));
        }

        public string AvatarChar => string.IsNullOrEmpty(Login) ? "?" : Login[0].ToString();
        [ObservableProperty] public partial string MiddleName { get; set; } = string.Empty;
        [ObservableProperty] public partial string Password { get; set; } = "********";
        [ObservableProperty] public partial string FirstName { get; set; } = string.Empty;
        [ObservableProperty] public partial string LastName { get; set; } = string.Empty;
        [ObservableProperty] public partial string DateJoined { get; set; } = string.Empty;
        [ObservableProperty] public partial bool? IsActive { get; set; }
        
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
        public void PasswordReset()
        {
            WeakReferenceMessenger.Default.Send(new OpenOrClosePasswordResetMessage(UserId, Login));
        }

        [RelayCommand]
        private async Task ChangeActiveUserAsync()
        {
            var newIsActive = IsActive ?? true;

            Log.Information(
                "Toggle user active state requested. UserId={UserId}, Login={Login}, NewIsActive={IsActive}",
                UserId,
                Login,
                newIsActive);
            
            try
            {
                await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
                const string sql = "UPDATE public.users SET is_active = @is_active WHERE id = @id";
                var rows = await connection.ExecuteScalarAsync(sql, new { id = UserId, is_active = newIsActive});
                
                Log.Information(
                    "User active state updated in database. UserId={UserId}, Login={Login}, NewIsActive={IsActive}, RowsAffected={RowsAffected}",
                    UserId,
                    Login,
                    newIsActive,
                    rows);
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "Failed to toggle user active state for UserId={UserId}, Login={Login}, TargetIsActive={IsActive}",
                    UserId,
                    Login,
                    newIsActive);
            }
        }
    }
}
