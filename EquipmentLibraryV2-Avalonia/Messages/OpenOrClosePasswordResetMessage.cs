namespace EquipmentLibraryV2_Avalonia.Messages;

public record OpenOrClosePasswordResetMessage(long? UserId = null, string? Login = null);