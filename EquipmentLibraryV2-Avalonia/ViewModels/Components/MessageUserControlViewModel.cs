using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components;

public partial class MessageUserControlViewModel : ViewModelBase
{
        private static readonly IBrush ErrorBrush =
        new SolidColorBrush(Color.Parse("#DB5C5C"));

    private static readonly IBrush WarningBrush =
        new SolidColorBrush(Color.Parse("#E8A838"));

    private static readonly IBrush SuccessBrush =
        new SolidColorBrush(Color.Parse("#59B76B"));

    private static readonly IBrush InformationBrush =
        new SolidColorBrush(Color.Parse("#579DE0"));

    private static readonly IBrush NeutralBrush =
        new SolidColorBrush(Color.Parse("#A8ADB5"));

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AccentBrush))]
    [NotifyPropertyChangedFor(nameof(TypeTitle))]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    [NotifyPropertyChangedFor(nameof(IconPath))]
    public partial NotificationType Type { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DisplayTitle))]
    public partial string Title { get; set; } = string.Empty;

    [ObservableProperty]
    public partial object? Object { get; set; }

    [ObservableProperty]
    public partial bool IsClosing { get; set; }

    public IBrush AccentBrush => Type switch
    {
        NotificationType.Error => ErrorBrush,
        NotificationType.Warning => WarningBrush,
        NotificationType.Success => SuccessBrush,
        NotificationType.Info => InformationBrush,
        _ => NeutralBrush
    };

    public string TypeTitle => Type switch
    {
        NotificationType.Error => "Ошибка",
        NotificationType.Warning => "Предупреждение",
        NotificationType.Success => "Успешно",
        NotificationType.Info => "Информация",
        _ => "Уведомление"
    };

    public string DisplayTitle =>
        string.IsNullOrWhiteSpace(Title) ? TypeTitle : Title;

    public string IconPath => Type switch
    {
        NotificationType.Error => "/Assets/status/error.svg",
        NotificationType.Warning => "/Assets/status/warning.svg",
        NotificationType.Success => "/Assets/status/success.svg",
        NotificationType.Info => "/Assets/status/info.svg",
        _ => "/Assets/status/info.svg"
    };

    [RelayCommand]
    public async Task Close()
    {
        if (IsClosing)
            return;

        IsClosing = true;

        await Task.Delay(300);

        WeakReferenceMessenger.Default.Send(
            new ShowOrHideNotification(ErrorAction.Remove, this));
    }
}