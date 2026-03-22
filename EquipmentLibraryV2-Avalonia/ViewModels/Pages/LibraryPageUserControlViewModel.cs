using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Pages
{
    public partial class LibraryPageUserControlViewModel : ViewModelBase
    {
        [ObservableProperty] private string? _searchText;
        [ObservableProperty] private DateTime _dateTime;
    }
}
