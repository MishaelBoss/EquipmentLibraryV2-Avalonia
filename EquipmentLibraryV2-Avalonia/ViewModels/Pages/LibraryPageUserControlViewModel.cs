using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Pages
{
    public partial class LibraryPageUserControlViewModel : ViewModelBase
    {
        [ObservableProperty] public partial string? SearchText { get; set; }
        [ObservableProperty] public partial DateTime DateTime { get; set; }
    }
}
