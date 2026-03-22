using Avalonia.Media.Imaging;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Scripts;
using System;
using System.Threading.Tasks;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components
{
    public partial class AuthorizationUserControlViewModel : ViewModelBase
    {
        [ObservableProperty] private string _messageError = string.Empty;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _login = string.Empty;
        [ObservableProperty][NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))] private string _password = string.Empty;
        [ObservableProperty] private Bitmap? _eyeIcon;
        [ObservableProperty] private bool _isPasswordVisible;

        public bool IsActiveConfirmButton
            => !string.IsNullOrEmpty(Login)
            && !string.IsNullOrEmpty(Password);

        private void UpdateEyeIcon()
        {
            var uri = new Uri(IsPasswordVisible ? "avares://EquipmentLibraryV2_Avalonia/Assets/eye-show-64.png" : "avares://EquipmentLibraryV2_Avalonia/Assets/eye-hide-64.png");

            using var stream = AssetLoader.Open(uri);
            EyeIcon = new Bitmap(stream);
        }

        [RelayCommand]
        public void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
            UpdateEyeIcon();
        }

        [RelayCommand]
        private async Task Authorization()
        {
            if (await AuthService.LoginAsync(Login, Password))
            {
                WeakReferenceMessenger.Default.Send(new OpenOrCloseAuthorizationMessage());
                ClearForm();
            }
            else
            {
                MessageError = "Неверный логин или пароль";
            }
        }

        private void ClearForm()
        {
            MessageError = string.Empty;
            Login = string.Empty;
            Password = string.Empty;
        }
    }
}
