using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dapper;
using EquipmentLibraryV2_Avalonia.Scripts;
using Npgsql;
using Serilog;
using System;
using System.Threading.Tasks;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components
{
    public partial class CartUserViewModel : ViewModelBase
    {
        public long UserId { get; init; }
        public int UserRole { get; init; }
        [ObservableProperty]private string _login = string.Empty;
        [ObservableProperty]private string _middleName = string.Empty;
        [ObservableProperty]private string _password = string.Empty;
        [ObservableProperty]private string _firstName = string.Empty;
        [ObservableProperty]private string _lastName = string.Empty;
        [ObservableProperty]private string _dateJoined = string.Empty;
        [ObservableProperty]private bool? _isActive;

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
        public void View() 
        { 
        }

        [RelayCommand]
        public void Delete()
        {
            WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmDeleteMessage(UserId, Login, "DELETE FROM public.users WHERE id = @id", () => WeakReferenceMessenger.Default.Send(new RefreshUserListMessage())));
        }

        public static Task<bool> IsAdministrator 
            => AuthService.TryAutoLoginAsync();

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
                Log.Error(ex.Message);
            }
        }
    }
}
