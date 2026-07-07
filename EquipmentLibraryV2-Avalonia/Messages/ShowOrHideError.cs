using EquipmentLibraryV2_Avalonia.Models;
using EquipmentLibraryV2_Avalonia.ViewModels;

namespace EquipmentLibraryV2_Avalonia.Messages;

public record ShowOrHideError(ErrorAction Action, ViewModelBase ViewModel);
