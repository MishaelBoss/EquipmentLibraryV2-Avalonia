using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;
using EquipmentLibraryV2_Avalonia.ViewModels.Pages;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using EquipmentLibraryV2_Avalonia.Services;

namespace EquipmentLibraryV2_Avalonia.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase,
        IRecipient<ShowOrHideError>,
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
        [ObservableProperty] public partial ViewModelBase? CurrentPage { get; set; }
        [ObservableProperty] public partial ViewModelBase? OverlayContent { get; set; }
        [ObservableProperty] public partial ViewModelBase? TopOverlayContent { get; set; }
        public ObservableCollection<ViewModelBase> ErrorMessages { get; } = [];

        [ObservableProperty] public partial string Version { get; set; }
        [ObservableProperty] public partial string StatusNetwork { get; set; } = "Проверка...";
        [ObservableProperty] public partial bool IsConnected { get; set; }

        private readonly AdminPanelPageUserControlViewModel _adminPanelPageUserControlViewModel = new();
        private readonly LibraryPageUserControlViewModel _libraryPageUserControlView = new();
        private readonly WorkAreaUserControlViewModel _workAreaUserControlViewModel = new();
        private readonly MeasurementRegisterPageUserControlViewModel _measurementRegisterPageUserControlViewModel = new();
        private readonly RegisterOfTestingEquipmentPageUserControlViewModel _registerOfTestingEquipmentPageUserControlViewModel = new();

        private readonly AuthorizationUserControlViewModel _authorizationUserControlViewModel = new();
        //private readonly AddOrEditUserUserControlViewModel _addOrEditUserUserControlViewModel = new();

        public RightBoardUserControlViewModel RightBoardViewModel { get; }

        [RelayCommand]
        public async Task ReturnToConnectNetwork()
        {
            await ConnectivityService.ConnectivityChecker();
        }

        public MainWindowViewModel()
        {
            CurrentPage = _libraryPageUserControlView;

            WeakReferenceMessenger.Default.RegisterAll(this);
            RightBoardViewModel = new RightBoardUserControlViewModel();

            Task.Run(async () =>
            {
                await LoadVersion();
                await CheckNetworkAsync();
            });
        }

        private async Task LoadVersion()
        {
            var handler = new SocketsHttpHandler
            {
                PooledConnectionLifetime = TimeSpan.FromMinutes(1),
                ConnectTimeout = TimeSpan.FromSeconds(5)
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
                
                if (jsonNode != null)
                {
                    var latest = jsonNode["promos"]?["latest"]?.ToString() ?? "Не найдено";
                    var recommended = jsonNode["promos"]?["recommended"]?.ToString() ?? "Не найдено";

                    Console.WriteLine($"Latest Version: {latest}");
                    Console.WriteLine($"Recommended Version: {recommended}");
                    
                    Version = "EquipmentLibrary v2: " + recommended;

                    var detailObject = jsonNode["detail"]?.AsObject();
                    if (detailObject != null)
                    {
                        Console.WriteLine("Details:");
                        foreach (var property in detailObject)
                        {
                            var versionKey = property.Key;
                            var description = property.Value?.ToString() ?? "";
                    
                            Console.WriteLine($" - {versionKey}: {description}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        private async Task CheckNetworkAsync()
        {
            StatusNetwork = "Проверка...";
            IsConnected = await ConnectivityService.ConnectivityChecker();
            StatusNetwork = IsConnected ? "Подключено" : "Нет соединения";
        }

        public void Receive(ShowOrHideError message)
        {
            if (message.Action == ErrorAction.Add)
            {
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
                message.Title);

            TopOverlayContent = new ConfirmDeleteUserControlViewModel(
                message.Id,
                message.Title,
                message.DeleteSql,
                message.OnSuccessCallback,
                message.AdditionalQueries);
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
