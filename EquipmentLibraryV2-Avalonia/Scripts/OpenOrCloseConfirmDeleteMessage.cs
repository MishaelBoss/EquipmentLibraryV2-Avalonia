using System;

namespace EquipmentLibraryV2_Avalonia.Scripts;

public record OpenOrCloseConfirmDeleteMessage(long? id = null, string? title = null, string? deleteSql = null, Action? onSuccessCallback = null, string[]? additionalQueries = null)
{
    public long Id { get; set; } = id ?? 0;
    public string Title { get; set; } = title ?? string.Empty;
    public string DeleteSql { get; set; } = deleteSql ?? string.Empty;
    public Action? OnSuccessCallback { get; set; } = onSuccessCallback ?? null;
    public string[]? AdditionalQueries { get; set; } = additionalQueries ?? [];
}
