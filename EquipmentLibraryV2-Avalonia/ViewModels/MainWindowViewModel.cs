using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;
using EquipmentLibraryV2_Avalonia.ViewModels.Pages;
using Serilog;

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
        [ObservableProperty] public partial ViewModelBase? ErrorContent { get; set; }

        private readonly AdminPanelPageUserControlViewModel _adminPanelPageUserControlViewModel = new();
        private readonly LibraryPageUserControlViewModel _libraryPageUserControlView = new();
        private readonly WorkAreaUserControlViewModel _workAreaUserControlViewModel = new();
        private readonly MeasurementRegisterPageUserControlViewModel _measurementRegisterPageUserControlViewModel = new();
        private readonly RegisterOfTestingEquipmentPageUserControlViewModel _registerOfTestingEquipmentPageUserControlViewModel = new();

        private readonly AuthorizationUserControlViewModel _authorizationUserControlViewModel = new();
        //private readonly AddOrEditUserUserControlViewModel _addOrEditUserUserControlViewModel = new();

        public RightBoardUserControlViewModel RightBoardViewModel { get; }

        public MainWindowViewModel()
        {
            CurrentPage = _libraryPageUserControlView;

            WeakReferenceMessenger.Default.RegisterAll(this);
            RightBoardViewModel = new RightBoardUserControlViewModel();
        }

        public void Receive(ShowOrHideError message)
        {
            ErrorContent = ErrorContent == null ? message.ViewModelBase : null;
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
