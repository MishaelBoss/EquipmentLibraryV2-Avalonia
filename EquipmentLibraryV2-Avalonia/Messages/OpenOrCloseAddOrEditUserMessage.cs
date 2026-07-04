namespace EquipmentLibraryV2_Avalonia.Messages;

public record OpenOrCloseAddOrEditUserMessage(
    long? Id = null,
    string? Login = null,
    string? FirstName = null,
    string? LastName = null,
    string? Password = null,
    int? UserRole = null
);
