using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Threading;
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
        private string _newVersion = string.Empty;
        private string _releaseNotes = string.Empty;
        private string _releaseDate = string.Empty;

        private readonly Lazy<AdminPanelPageUserControlViewModel> _adminPanel = new(() =>
            new AdminPanelPageUserControlViewModel());
        
        private readonly Lazy<LibraryPageUserControlViewModel> _library = new(() =>
            new LibraryPageUserControlViewModel());
        
        private readonly Lazy<WorkAreaUserControlViewModel> _workArea = new(() => 
            new WorkAreaUserControlViewModel());
        
        private readonly Lazy<MeasurementRegisterPageUserControlViewModel> _measurementRegister =
            new(() => new MeasurementRegisterPageUserControlViewModel());

        private readonly Lazy<RegisterOfTestingEquipmentPageUserControlViewModel> _registerOfTestingEquipment =
            new(() => new RegisterOfTestingEquipmentPageUserControlViewModel());

        private readonly AuthorizationUserControlViewModel _authorization = new();

        public RightBoardUserControlViewModel RightBoardViewModel { get; }

        private readonly AppSettings _settings;

        [RelayCommand]
        public async Task ReturnToConnectNetwork()
        {
            await ConnectivityService.ConnectivityChecker();
            await CheckNetworkAsync();
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
        public async Task Update(Visual visualContext)
        {
            var data = new DetailUpdateDialog(_newVersion, _releaseNotes, _releaseDate);
            var dialog = new DetailUpdateDialogWindow
            {
                DataContext = new DetailUpdateDialogWindowViewModel(data)
            };

            if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime { MainWindow: { } mainWindow })
            {
                await dialog.ShowDialog(mainWindow);
            }
        }
        
        public MainWindowViewModel()
        {
            IsLoading = true;
            CurrentPage = _library.Value;
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
                var response = await client.GetAsync("https://pastebin.com/raw/5CArBfWP");
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
                var targetVersionDetail = string.Empty;
                var targetVersionDate = string.Empty;
                
                var detailObject = jsonNode["detail"]?.AsObject();
                if (detailObject != null && !string.IsNullOrWhiteSpace(targetVersion))
                {
                    var versionInfoNode = detailObject[targetVersion]?.AsObject();
                    
                    if (versionInfoNode != null)
                    {
                        targetVersionDetail = versionInfoNode["description"]?.ToString() ?? string.Empty;
                        targetVersionDate = versionInfoNode["date"]?.ToString() ?? string.Empty;
                        
                        Log.Debug("Found detail for {TargetVersion}. Date: {Date}, Detail: {Detail}", 
                            targetVersion, targetVersionDate, targetVersionDetail);
                    }
                    else
                    {
                        Log.Debug("No detail found in manifest for target version {TargetVersion}", targetVersion);
                    }
                }
                
                if (!string.IsNullOrWhiteSpace(targetVersion) &&
                    VersionHelper.IsNewerVersion(currentVersion, targetVersion))
                {
                    IsNewVerion = true;
                    Version = $"EquipmentLibrary v2: {targetVersion}";
                    _newVersion = $"EquipmentLibrary v2: {targetVersion}";
                    _releaseNotes = string.IsNullOrEmpty(targetVersionDetail) 
                        ? "There are no change logs for this version."
                        : targetVersionDetail;
                    _releaseDate = targetVersionDate;
                    
                    Log.Information("Update found: {Version}. Description: {Description}", targetVersion, targetVersionDetail);
                }
                else
                {
                    IsNewVerion = false;
                    Version = AppConfig.DisplayVersion;
                    Log.Information("No update available for current version {CurrentVersion}", currentVersion);
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                Log.Warning(ex, "Version check timed out.");
                WeakReferenceMessenger.Default.Send(
                    new ShowOrHideNotification(ErrorAction.Add, new ErrorUserControlViewModel(), ("Version check timed out", 503L)));
            }
            catch (Exception ex)
            {
                WeakReferenceMessenger.Default.Send(
                    new ShowOrHideNotification(ErrorAction.Add, new ErrorUserControlViewModel(), ("Failed to fetch version info", 503L)));
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
                if (message.ViewModel is ErrorUserControlViewModel errorVm)
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
            CurrentPage = _library.Value;
        }

        public void Receive(OpenAdminPanelMessage message)
        {
            CurrentPage = _adminPanel.Value;
        }

        public void Receive(OpenLibraryMessage message)
        {
            CurrentPage = _library.Value;
        }

        public void Receive(OpenWorkAreaMessage message)
        {
            CurrentPage = _workArea.Value;
        }

        public void Receive(OpenOrCloseAuthorizationMessage message)
        {
            OverlayContent = OverlayContent == null ? _authorization : null;
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
            CurrentPage = _measurementRegister.Value;
        }

        public void Receive(OpenRegisterOfTestingEquipmentMessage message)
        {
            CurrentPage = _registerOfTestingEquipment.Value;
        }

        public void Dispose()
            => WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}
