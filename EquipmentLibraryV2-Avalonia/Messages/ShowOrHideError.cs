using EquipmentLibraryV2_Avalonia.ViewModels;

namespace EquipmentLibraryV2_Avalonia.Messages;

public enum ErrorAction
{
    Add,
    Remove
}

public record ShowOrHideError(ErrorAction Action, ViewModelBase ViewModel);
