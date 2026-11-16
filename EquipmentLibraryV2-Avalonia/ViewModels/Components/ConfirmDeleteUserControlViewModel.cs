using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;
using Npgsql;
using Serilog;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Services;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components;

public partial class ConfirmDeleteUserControlViewModel(
    long? id,
    string? title,
    Action onSuccessCallback,
    DeleteEntityType entityType = DeleteEntityType.User) : ViewModelBase
{
    public string Text { get; set; } = $"Delete: {title}";

    private static string GetDeleteSql(DeleteEntityType type) => type switch
    {
        DeleteEntityType.User => "DELETE FROM public.users WHERE id = @id",
        DeleteEntityType.Equipment => throw new NotSupportedException("Equipment deletion is not implemented."),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    [RelayCommand]
    public async Task Close()
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmDeleteMessage());
    }

    [RelayCommand]
    public async Task Confirm()
    {
        if (!AuthService.IsAdministrator)
        {
            Log.Warning("Delete denied: current session is not an administrator");
            WeakReferenceMessenger.Default.Send(
                new ShowOrHideNotification(
                    ErrorAction.Add,
                    new MessageUserControlViewModel
                    {
                        Type = NotificationType.Error,
                        Title = "Недостаточно прав",
                        Object = "Удаление записей разрешено только администратору."
                    }));
            return;
        }

        if (id == null)
        {
            return;
        }

        var deleteSql = GetDeleteSql(entityType);

        try
        {
            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            await connection.OpenAsync();

            var rowsAffected = 0;

            await using (var command = new NpgsqlCommand(deleteSql, connection))
            {
                command.Parameters.AddWithValue("@id", id);
                rowsAffected = await command.ExecuteNonQueryAsync();

                Log.Debug(
                    "Delete query executed: {EntityType}, rows affected: {RowsAffected}",
                    entityType,
                    rowsAffected);
            }

            if (rowsAffected > 0)
            {
                Log.Information("Successfully deleted records. Total rows affected: {RowsAffected}", rowsAffected);

                onSuccessCallback.Invoke();

                WeakReferenceMessenger.Default.Send(
                    new ShowOrHideNotification(
                        ErrorAction.Add,
                        new MessageUserControlViewModel
                        {
                            Type = NotificationType.Success,
                            Title = "Удалено",
                            Object = $"Удалена запись: {title}"
                        }));
            }
            else
            {
                Log.Error("No records found to delete for ID: {Id}", id);

                WeakReferenceMessenger.Default.Send(
                    new ShowOrHideNotification(
                        ErrorAction.Add,
                        new MessageUserControlViewModel
                        {
                            Type = NotificationType.Warning,
                            Title = "Запись не найдена",
                            Object = "Возможно, запись уже была удалена."
                        }));
            }

            WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmDeleteMessage());
        }
        catch (PostgresException pgEx)
        {
            Log.Error("Database error (SQLState: {PgExSqlState}): {PgExMessageText}", pgEx.SqlState, pgEx.MessageText);

            WeakReferenceMessenger.Default.Send(
                new ShowOrHideNotification(
                    ErrorAction.Add,
                    new MessageUserControlViewModel
                    {
                        Type = NotificationType.Error,
                        Title = "Не удалось удалить запись",
                        Object = pgEx.MessageText
                    }));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occured during confirm delete");

            WeakReferenceMessenger.Default.Send(
                new ShowOrHideNotification(
                    ErrorAction.Add,
                    new MessageUserControlViewModel
                    {
                        Type = NotificationType.Error,
                        Title = "Не удалось удалить запись",
                        Object = ex.Message
                    }));
        }
    }
}
