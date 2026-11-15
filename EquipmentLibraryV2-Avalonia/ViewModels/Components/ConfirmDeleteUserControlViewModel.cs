using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;
using Npgsql;
using Serilog;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Services;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components;

public partial class ConfirmDeleteUserControlViewModel(long? id, string? title, string? deleteSql, Action onSuccessCallback, string[]? additionalQueries = null) : ViewModelBase
{
    public string Text { get; set; } = $"Delete: {title}";

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
            
        try
        {
            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            await connection.OpenAsync();

            NpgsqlTransaction? transaction = null;
            if (additionalQueries is not null && additionalQueries.Length > 0)
            {
                transaction = await connection.BeginTransactionAsync();
            }

            try
            {
                var totalRowsAffected = 0;

                if (additionalQueries is not null && additionalQueries.Length > 0)
                {
                    foreach (var query in additionalQueries)
                    {
                        try
                        {
                            await using var command = new NpgsqlCommand(query, connection, transaction);
                            command.Parameters.AddWithValue("@id", id);
                            var rows = await command.ExecuteNonQueryAsync();
                            totalRowsAffected += rows;

                            Log.Debug("Additional query executed: {Query}, rows affected: {Rows}", query, rows);
                        }
                        catch (Exception ex)
                        {
                            Log.Error("Failed to execute additional query: {Query} {ExMessage}", query, ex.Message);
                            throw;
                        }
                    }
                }

                await using (var mainCommand = new NpgsqlCommand(deleteSql, connection))
                {
                    if (transaction is not null)
                    {
                        mainCommand.Transaction = transaction;
                    }

                    mainCommand.Parameters.AddWithValue("@id", id);
                    var mainRows = await mainCommand.ExecuteNonQueryAsync();
                    totalRowsAffected += mainRows;
                            
                    Log.Debug("Main query executed: {DeleteSql}, rows affected: {MainRows}", deleteSql, mainRows);
                }

                if (transaction is not null)
                {
                    await transaction.CommitAsync();
                }

                if (totalRowsAffected > 0)
                {
                    Log.Information("Successfully deleted records. Total rows affected: {TotalRowsAffected}", totalRowsAffected);

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
            catch (Exception)
            {
                if (transaction is not null)
                {
                    await transaction.RollbackAsync();
                }
                throw;
            }
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