using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;
using EquipmentLibraryV2_Avalonia.ViewModels.Settings;
using EquipmentLibraryV2_Avalonia.Services.Interfaces;
using EquipmentLibraryV2_Avalonia.Models;

namespace EquipmentLibraryV2_Avalonia.ViewModels;

public partial class SettingsDialogWindowViewModel : ViewModelBase
{
    private readonly AppSettings _settings;
    public ObservableCollection<SettingsPageItem> Pages { get; }
    public ObservableCollection<SettingsSearchResult> SearchResults { get; } = [];
    private readonly List<SettingsSearchResult> _searchIndex = [];

    public event Action<string>? NavigationRequested;

    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsSearching { get; set; }
    [ObservableProperty] public partial bool NoSearchResults { get; set; }
    [ObservableProperty] public partial SettingsSearchResult? SelectedSearchResult { get; set; }
    [ObservableProperty] public partial SettingsPageItem? SelectedPage { get; set; }

    [ObservableProperty] public partial bool HasChanges { get; set; }

    public SettingsDialogWindowViewModel()
    {
        _settings = AppSettings.Load();
        
        Pages =
        [
            new SettingsPageItem("Логирование", new LoggingViewModel()),
            new SettingsPageItem("Расположение папок", new FolderLocationUserControlViewModel(_settings)),
            new SettingsPageItem("Обновления", new UpdatesUserControlViewModel(_settings)),
            new SettingsPageItem("Подключение", new ConnectionUserControlViewModel(_settings))
        ];

        foreach (var page in Pages)
            _searchIndex.Add(new SettingsSearchResult(page.Title, page, null));

        _searchIndex.AddRange(
        [
            new SettingsSearchResult("Журналы работы приложения", Pages[0], "Logs"),
            new SettingsSearchResult("Сохранять файлы в корень папки напрямую", Pages[1], "UseRawSelectedFolder"),
            new SettingsSearchResult("Расположение сохраненных документов", Pages[1], "DownloadFolder"),
            new SettingsSearchResult("Предлагать последние обновления", Pages[2], "CheckLatestUpdates"),
            new SettingsSearchResult("IP адрес сервера", Pages[3], "Ip"),
            new SettingsSearchResult("Порт", Pages[3], "Port"),
            new SettingsSearchResult("База данных", Pages[3], "Database"),
            new SettingsSearchResult("Пользователь", Pages[3], "User"),
            new SettingsSearchResult("Пароль", Pages[3], "Password"),
            new SettingsSearchResult("Проверить подключение", Pages[3], "TestConnection")
        ]);

        SelectedPage = Pages[0];

        foreach (var page in Pages)
        {
            if (page.ViewModel is INotifyPropertyChanged npc)
                npc.PropertyChanged += OnPagePropertyChanged;
        }
    }

    partial void OnSearchTextChanged(string? value)
    {
        var terms = (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        IsSearching = terms.Length > 0;
        SearchResults.Clear();

        if (IsSearching)
        {
            foreach (var result in _searchIndex.Where(result => terms.All(term =>
                         result.Title.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
                         result.Page.Title.Contains(term, StringComparison.CurrentCultureIgnoreCase))))
                SearchResults.Add(result);
        }

        NoSearchResults = IsSearching && SearchResults.Count == 0;
    }

    partial void OnSelectedSearchResultChanged(SettingsSearchResult? value)
    {
        if (value is null) return;
        SelectedPage = value.Page;
        if (value.Target is not null)
            NavigationRequested?.Invoke(value.Target);
        SelectedSearchResult = null;
    }

    private void OnPagePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RecalculateHasChanges();
    }

    private void RecalculateHasChanges()
    {
        foreach (var page in Pages)
        {
            if (page.ViewModel is not ISettingsPage { HasChanges: true }) continue;
            HasChanges = true;
            return;
        }
        HasChanges = false;
    }

    [RelayCommand]
    public void Cancel(Window? window)
    {
        window?.Close();
    }

    [RelayCommand]
    public void Apply()
    {
        foreach (var page in Pages)
        {
            if (page.ViewModel is ISettingsPage sp)
                sp.Save();
        }
        _settings.Save();
        
        RecalculateHasChanges();
    }

    [RelayCommand]
    public void Ok(Window? window)
    {
        Apply();

        WeakReferenceMessenger.Default.Send(
            new ShowOrHideNotification(
                ErrorAction.Add,
                new MessageUserControlViewModel
                {
                    Type = NotificationType.Success,
                    Title = "Настройки сохранены",
                    Object = "Изменения применены."
                }));

        window?.Close();
    }
}
