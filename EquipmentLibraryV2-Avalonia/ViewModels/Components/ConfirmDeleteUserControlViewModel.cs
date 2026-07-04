using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;
using Npgsql;
using Serilog;
using System;
using System.Threading.Tasks;
using EquipmentLibraryV2_Avalonia.Infrastructure;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components
{
    public partial class ConfirmDeleteUserControlViewModel(long id, string title, string deleteSql, Action onSuccessCallback, string[]? additionalQueries = null) : ViewModelBase
    {
        public string Text { get; set; } = $"Delete: {title}";

        private readonly Action _onSuccessCallback = onSuccessCallback;
        private readonly string _deleteSql = deleteSql;
        private readonly string[]? _additionalQueries = additionalQueries;

        [RelayCommand]
        public async Task Close()
        {
            WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmDeleteMessage());
        }

        [RelayCommand]
        public async Task Confirm() 
        {
            try
            {
                await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
                await connection.OpenAsync();

                NpgsqlTransaction? transaction = null;
                if (_additionalQueries is not null && _additionalQueries.Length > 0)
                {
                    transaction = await connection.BeginTransactionAsync();
                }

                try
                {
                    var totalRowsAffected = 0;

                    if (_additionalQueries is not null && _additionalQueries.Length > 0)
                    {
                        foreach (var query in _additionalQueries)
                        {
                            try
                            {
                                await using var command = new NpgsqlCommand(query, connection, transaction);
                                command.Parameters.AddWithValue("@id", id);
                                var rows = await command.ExecuteNonQueryAsync();
                                totalRowsAffected += rows;

                                Log.Debug($"Additional query executed: {query}, rows affected: {rows}");
                            }
                            catch (Exception ex)
                            {
                                Log.Error($"Failed to execute additional query: {query} {ex.Message}");
                                throw;
                            }
                        }
                    }

                    await using (var mainCommand = new NpgsqlCommand(_deleteSql, connection))
                    {
                        if (transaction is not null)
                        {
                            mainCommand.Transaction = transaction;
                        }

                        mainCommand.Parameters.AddWithValue("@id", id);
                        var mainRows = await mainCommand.ExecuteNonQueryAsync();
                        totalRowsAffected += mainRows;

                        Log.Debug($"Main query executed: {_deleteSql}, rows affected: {mainRows}");
                    }

                    if (transaction is not null)
                    {
                        await transaction.CommitAsync();
                    }

                    if (totalRowsAffected > 0)
                    {
                        Log.Information($"Successfully deleted records. Total rows affected: {totalRowsAffected}");

                        _onSuccessCallback.Invoke();

                        WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmDeleteMessage());
                    }
                    else
                    {
                        Log.Error($"No records found to delete for ID: {id}");

                        WeakReferenceMessenger.Default.Send(new OpenOrCloseConfirmDeleteMessage());
                    }
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
                Log.Error($"Database error (SQLState: {pgEx.SqlState}): {pgEx.MessageText}");
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}
