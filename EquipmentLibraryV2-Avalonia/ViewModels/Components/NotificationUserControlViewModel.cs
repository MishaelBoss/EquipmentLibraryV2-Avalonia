using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components;

/// <summary>
/// Боковая панель уведомлений: история всех уведомлений текущей сессии.
/// Коллекция общая с попапами (MainWindowViewModel), поэтому панель и тосты
/// всегда показывают одни и те же объекты.
/// </summary>
public partial class NotificationUserControlViewModel : ViewModelBase
{
    public ObservableCollection<MessageUserControlViewModel> Messages { get; }

    public bool HasMessages => Messages.Count > 0;

    public bool IsEmpty => !HasMessages;

    public int UnreadCount => Messages.Count(message => !message.IsRead);

    public bool HasUnread => UnreadCount > 0;

    public NotificationUserControlViewModel()
        : this([])
    {
    }

    public NotificationUserControlViewModel(ObservableCollection<MessageUserControlViewModel> messages)
    {
        Messages = messages;
        Messages.CollectionChanged += OnMessagesChanged;

        foreach (var message in Messages)
            message.PropertyChanged += OnMessagePropertyChanged;
    }

    /// <summary>Открытие панели помечает все уведомления прочитанными.</summary>
    public void MarkAllAsRead()
    {
        foreach (var message in Messages)
            message.IsRead = true;
    }

    [RelayCommand]
    public void Hide()
        => WeakReferenceMessenger.Default.Send(new OpenOrCloseNotificationMessage());

    [RelayCommand]
    private void ClearAll()
    {
        foreach (var message in Messages.ToList())
            WeakReferenceMessenger.Default.Send(new ShowOrHideNotification(ErrorAction.Remove, message));
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (MessageUserControlViewModel message in e.OldItems)
                message.PropertyChanged -= OnMessagePropertyChanged;
        }

        if (e.NewItems is not null)
        {
            foreach (MessageUserControlViewModel message in e.NewItems)
                message.PropertyChanged += OnMessagePropertyChanged;
        }

        RaiseCounters();
    }

    private void OnMessagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MessageUserControlViewModel.IsRead))
            RaiseCounters();
    }

    private void RaiseCounters()
    {
        OnPropertyChanged(nameof(HasMessages));
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(UnreadCount));
        OnPropertyChanged(nameof(HasUnread));
    }
}
