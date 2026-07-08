using System;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Svg.Skia;
using Avalonia.Threading;
using EquipmentLibraryV2_Avalonia.Services;
using Serilog;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components
{
    public partial class AuthorizationUserControlViewModel : ViewModelBase
    {
        private readonly IImage[] _animationFrames = new IImage[8];
        
        private CancellationTokenSource? _animationCts;
        
        [ObservableProperty]
        public partial string MessageError { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))]
        public partial string Login { get; set; } = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))]
        public partial string Password { get; set; } = string.Empty;
        
        [ObservableProperty] 
        public partial bool IsPasswordVisible { get; set; }
        
        [ObservableProperty]
        public partial bool IsLoading { get; set; }
        
        [ObservableProperty]
        public partial IImage CurrentAnimationFrame { get; set; }
        
        public bool IsActiveConfirmButton
            => !string.IsNullOrEmpty(Login)
            && !string.IsNullOrEmpty(Password)
            && !IsLoading;

        [RelayCommand]
        public void Close()
        {
            WeakReferenceMessenger.Default.Send(new OpenOrCloseAuthorizationMessage());
        }

        [RelayCommand]
        public void TogglePasswordVisibility()
        {
            IsPasswordVisible = !IsPasswordVisible;
        }

        public AuthorizationUserControlViewModel()
        {
            for (var i = 1; i <= 8; i++)
            {
                var path = $"avares://EquipmentLibraryV2_Avalonia/Assets/process/step_{i}.svg";

                try
                {
                    var svgSource = SvgSource.Load(path);
                    _animationFrames[i - 1] = new SvgImage
                    {
                        Source = svgSource
                    };
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to load SVG frame {FrameIndex} from {Path}", i, path);
                    _animationFrames[i - 1] = null!;
                }
            }
            
            CurrentAnimationFrame = _animationFrames[0];
        }

        [RelayCommand]
        private async Task Authorization()
        {
            if (IsLoading) return;
            
            IsLoading = true;
            MessageError = string.Empty;
            
            _animationCts = new CancellationTokenSource();
            _ = RunAnimationAsync(_animationCts.Token);
            
            try
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
            catch (Exception ex)
            {
                MessageError = "Ошибка соединения с сервером";
                Log.Warning(ex, "Authorization failed due to exception");
            }
            finally
            {
                _animationCts.Cancel();
                IsLoading = false;
                CurrentAnimationFrame = _animationFrames[0]; 
            }
        }

        private async Task RunAnimationAsync(CancellationToken token)
        {
            var currentFrameIndex = 0;

            while (!token.IsCancellationRequested)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var frame = _animationFrames[currentFrameIndex];
                    CurrentAnimationFrame = frame;
                });

                currentFrameIndex++;
                if (currentFrameIndex >= _animationFrames.Length)
                {
                    currentFrameIndex = 0;
                }

                try
                {
                    await Task.Delay(150, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
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
