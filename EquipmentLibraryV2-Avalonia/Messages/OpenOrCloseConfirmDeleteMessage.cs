using EquipmentLibraryV2_Avalonia.Models;

namespace EquipmentLibraryV2_Avalonia.Messages;

public record OpenOrCloseConfirmDeleteMessage(long? Id = null, string? Title = null, DeleteEntityType EntityType = DeleteEntityType.User, Action? OnSuccessCallback = null, string[]? AdditionalQueries = null);