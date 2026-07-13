namespace EquipmentLibraryV2_Avalonia.Messages;

public record OpenOrCloseConfirmDeleteMessage(long? Id = null, string? Title = null, string? DeleteSql = null, Action? OnSuccessCallback = null, string[]? AdditionalQueries = null);