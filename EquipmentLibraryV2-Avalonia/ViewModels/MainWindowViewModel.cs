using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Helpers;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;
using EquipmentLibraryV2_Avalonia.Services;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;
using EquipmentLibraryV2_Avalonia.ViewModels.Pages;
using EquipmentLibraryV2_Avalonia.Views;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace EquipmentLibraryV2_Avalonia.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase,
        IRecipient<ShowOrHideNotification>,
        IRecipient<LogoutMessage>,
        IRecipient<OpenAdminPanelMessage>, 
        IRecipient<OpenLibraryMessage>, 
        IRecipient<OpenWorkAreaMessage>, 
        IRecipient<OpenOrCloseAuthorizationMessage>, 
        IRecipient<OpenOrCloseAddOrEditUserMessage>,
        IRecipient<OpenOrCloseConfirmDeleteMessage>,
        IRecipient<OpenMeasurementRegisterMessage>,
        IRecipient<OpenRegisterOfTestingEquipmentMessage>
    {
        [ObservableProperty] public partial bool IsLoading { get; set; }

        [ObservableProperty] public partial ViewModelBase? CurrentPage { get; set; }
        [ObservableProperty] public partial ViewModelBase? OverlayContent { get; set; }
        [ObservableProperty] public partial ViewModelBase? TopOverlayContent { get; set; }
        public ObservableCollection<ViewModelBase> ErrorMessages { get; } = [];

        [ObservableProperty] public partial string Version { get; set; } = AppConfig.Version;
        [ObservableProperty] public partial string StatusNetwork { get; set; } = "Проверка...";
        [ObservableProperty] public partial bool IsConnected { get; set; }
        [ObservableProperty] public partial bool IsNewVerion { get; set; } = false;

        private readonly AdminPanelPageUserControlViewModel _adminPanelPageUserControlViewModel = new();
        private readonly LibraryPageUserControlViewModel _libraryPageUserControlView = new();
        private readonly WorkAreaUserControlViewModel _workAreaUserControlViewModel = new();
        private readonly MeasurementRegisterPageUserControlViewModel _measurementRegisterPageUserControlViewModel = new();
        private readonly RegisterOfTestingEquipmentPageUserControlViewModel _registerOfTestingEquipmentPageUserControlViewModel = new();

        private readonly AuthorizationUserControlViewModel _authorizationUserControlViewModel = new();
        //private readonly AddOrEditUserUserControlViewModel _addOrEditUserUserControlViewModel = new();

        public RightBoardUserControlViewModel RightBoardViewModel { get; }

        private readonly AppSettings _settings;

        [RelayCommand]
        public async Task ReturnToConnectNetwork()
        {
            await ConnectivityService.ConnectivityChecker();
        }

        [RelayCommand]
        public async Task About() {
            var dialog = new AboutDialogWindow();

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var mainWindow = desktop.MainWindow;

                if (mainWindow != null)
                {
                    await dialog.ShowDialog(mainWindow);
                }
            }
        }

        [RelayCommand]
        public async Task Update(Visual visualContext) {
            var topLevel = TopLevel.GetTopLevel(visualContext);

            if (topLevel?.Launcher is { } launcher)
                await launcher.LaunchUriAsync(new Uri("https://github.com/MishaelBoss/EquipmentLibraryV2-Avalonia"));
        }
        
        public MainWindowViewModel()
        {
            IsLoading = true;
            CurrentPage = _libraryPageUserControlView;
            _settings = AppSettings.Load();

            WeakReferenceMessenger.Default.RegisterAll(this);
            RightBoardViewModel = new RightBoardUserControlViewModel();
        }

        public async Task InitializeAsync()
        {
            Log.Information("Initialization started.");

            try {
                await Task.WhenAll(LoadVersion(), CheckNetworkAsync());
                Log.Information("Initialization completed successfully.");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Initialization failed.");
            }
            finally
            {
                IsLoading = false;
                Log.Information("Loading state set to false.");
            }
        }

        private async Task LoadVersion()
        {
            Version = AppConfig.DisplayVersion;
            Log.Information("Starting version check. Current version: {CurrentVersion}", AppConfig.Version);

            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(1),
                ConnectTimeout = TimeSpan.FromSeconds(10)
            };

            using var client = new HttpClient(handler);
            client.DefaultRequestVersion = HttpVersion.Version30;
            client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;

            try
            {
                var response = await client.GetAsync("https://raw.githubusercontent.com/MishaelBoss/EquipmentLibraryV2-Avalonia/refs/heads/main/version_project");
                response.EnsureSuccessStatusCode();

                var jsonString = await response.Content.ReadAsStringAsync();
                var jsonNode = JsonNode.Parse(jsonString);

                if (jsonNode == null)
                {
                    Log.Warning("Version manifest is empty or invalid JSON.");
                    return;
                }

                var latest = jsonNode["promos"]?["latest"]?.ToString();
                var recommended = jsonNode["promos"]?["recommended"]?.ToString();

                Log.Information("Remote versions loaded. Latest: {Latest}, Recommended: {Recommended}", latest, recommended);

                var currentVersion = AppConfig.Version;
                var targetVersion = _settings.CheckLatestUpdates ? latest : recommended;

                if (!string.IsNullOrWhiteSpace(targetVersion) &&
                    VersionHelper.IsNewerVersion(currentVersion, targetVersion))
                {
                    IsNewVerion = true;
                    Version = $"EquipmentLibrary v2: {targetVersion}";
                    Log.Information("Update found: {Version}", targetVersion);
                }
                else
                {
                    IsNewVerion = false;
                    Version = AppConfig.DisplayVersion;
                    Log.Information("No update available for current version {CurrentVersion}", currentVersion);
                }

                var detailObject = jsonNode["detail"]?.AsObject();
                if (detailObject != null)
                {
                    Log.Debug("Version details loaded: {Count} entries", detailObject.Count);
                    foreach (var property in detailObject)
                    {
                        Log.Debug("Detail {VersionKey}: {Description}", property.Key, property.Value?.ToString() ?? "");
                    }
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Log.Warning(ex, "Version check timed out.");
                WeakReferenceMessenger.Default.Send(
                    new ShowOrHideNotification(ErrorAction.Add, new ConnectionErrorUserControlViewModel(), ("Version check timed out", 503L)));
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(
                    new ShowOrHideNotification(ErrorAction.Add, new ConnectionErrorUserControlViewModel(), ("Failed to fetch version info", 503L)));
                Log.Warning(ex, "Failed to fetch version info");
                Version = AppConfig.DisplayVersion;
            }
        }

        private async Task CheckNetworkAsync()
        {
            StatusNetwork = "Проверка...";
            IsConnected = await ConnectivityService.ConnectivityChecker();
            StatusNetwork = IsConnected ? "Подключено" : "Нет соединения";
        }

        public void Receive(ShowOrHideNotification message)
        {
            if (message.Action == ErrorAction.Add)
            {
                if (message.ViewModel is ConnectionErrorUserControlViewModel errorVm)
                {
                    errorVm.Object = message.Data switch
                    {
                        (string text, long id)  => $"{text} (Cod: {id})",
                        string textOnly         => textOnly,
                        long codeOnly           => $"Cod error: {codeOnly}",
                        _                       => "An unknown error occurred"
                    };
                }

                if (!ErrorMessages.Contains(message.ViewModel))
                {
                    ErrorMessages.Add(message.ViewModel);
                }
            }
            else
            {
                ErrorMessages.Remove(message.ViewModel);
                if (message.ViewModel is IDisposable disposable)
                    disposable.Dispose();
            }
        }

        public void Receive(LogoutMessage message)
        {
            CurrentPage = _libraryPageUserControlView;
        }

        public void Receive(OpenAdminPanelMessage message)
        {
            CurrentPage = _adminPanelPageUserControlViewModel;
        }

        public void Receive(OpenLibraryMessage message)
        {
            CurrentPage = _libraryPageUserControlView;
        }

        public void Receive(OpenWorkAreaMessage message)
        {
            CurrentPage = _workAreaUserControlViewModel;
        }

        public void Receive(OpenOrCloseAuthorizationMessage message)
        {
            OverlayContent = OverlayContent == null ? _authorizationUserControlViewModel : null;
        }

        public void Receive(OpenOrCloseAddOrEditUserMessage message)
        {
            TopOverlayContent = TopOverlayContent == null ? new AddOrEditUserUserControlViewModel(message.Id, message.Login, message.FirstName, message.LastName, message.Password, message.UserRole) : null;
        }

        public void Receive(OpenOrCloseConfirmDeleteMessage message)
        {
            if (message.OnSuccessCallback is null)
            {
                Log.Debug("Received CloseConfirmDelete message. Overlay will be closed.");
                TopOverlayContent = null;
                return;
            }
            
            Log.Debug(
                "Received OpenConfirmDelete message. Id={Id}, Title={Title}",
                message.Id,
                message.Title
            );

            TopOverlayContent = new ConfirmDeleteUserControlViewModel(
                message.Id,
                message.Title,
                message.DeleteSql,
                message.OnSuccessCallback,
                message.AdditionalQueries
            );
        }

        public void Receive(OpenMeasurementRegisterMessage message)
        {
            CurrentPage = _measurementRegisterPageUserControlViewModel;
        }

        public void Receive(OpenRegisterOfTestingEquipmentMessage message)
        {
            CurrentPage = _registerOfTestingEquipmentPageUserControlViewModel;
        }

        ~MainWindowViewModel() 
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
        }
    }
}
