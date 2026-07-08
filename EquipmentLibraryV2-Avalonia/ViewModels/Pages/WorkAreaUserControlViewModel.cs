using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using EquipmentLibraryV2_Avalonia.Models;
using Npgsql;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Services;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Pages;

public partial class WorkAreaUserControlViewModel : ViewModelBase
{
    [ObservableProperty] public partial int TotalObjects { get; set; }
    [ObservableProperty] public partial ObservableCollection<EquipmentType> EquipmentTypes { get; set; } = [];
    [ObservableProperty] public partial EquipmentType? SelectedEquipmentType { get; set; }
    [ObservableProperty] public partial string Title { get; set; }
    [ObservableProperty] public partial string SerialNumber { get; set; }
    [ObservableProperty] public partial string Model { get; set; }
    [ObservableProperty] public partial string InvNumber { get; set; }

    [RelayCommand]
    public async Task Add()
    {
        Log.Information("Add command started. Title={Title}, EquipmentTypeId={EquipmentTypeId}", Title, SelectedEquipmentType?.Id);
        
        try
        {
            await AddAsync();
            Log.Information("Add command completed successfully. Title={Title}", Title);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Add command failed. Title={Title}, EquipmentTypeId={EquipmentTypeId}",
                Title, SelectedEquipmentType?.Id);
        }
    }

    public WorkAreaUserControlViewModel() 
    {
        Log.Information("WorkAreaUserControlViewModel created");
        
        Task.Run(async () =>
        {
            Log.Debug("Initial data refresh started");
            await RefreshDataAsync();
            Log.Debug("Initial data refresh finished");
        });
    }

    private async Task RefreshDataAsync() 
    {
        Log.Debug("RefreshDataAsync started");
        
        Dispatcher.UIThread.Post(() => {
            Log.Debug("Clearing EquipmentTypes on UI thread");
            EquipmentTypes.Clear();
        });

        await LoadEquipmentTypeAsync();
        await LoadAllObjectsAsync();
        
        Log.Debug("RefreshDataAsync completed");
    }

    private async Task LoadEquipmentTypeAsync() 
    {
        Log.Debug("LoadEquipmentTypeAsync started");
        
        try
        {
            await using var connection = new NpgsqlConnection(AppConfig.ConnectionString());
            const string sql = "SELECT id, type FROM public.equipment_type";
            
            Log.Debug("Executing SQL to load equipment types: {Sql}", sql);
            var data = (await connection.QueryAsync<EquipmentType>(sql)).ToList();
            
            Log.Information("Loaded {Count} equipment types from database", data.Count);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var item in data)
                {
                    EquipmentTypes.Add(item);
                }
                Log.Debug("EquipmentTypes collection updated on UI thread. Count={Count}", EquipmentTypes.Count);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load equipment types");
        }
    }

    private async Task LoadAllObjectsAsync()
    {
        Log.Debug("LoadAllObjectsAsync started");
        
        try
        {
            await using var connection = new NpgsqlConnection(AppConfig.ConnectionString());
            const string sql = "SELECT COUNT(*) FROM public.equipment";
            
            Log.Debug("Executing SQL to count equipment: {Sql}", sql);
            await connection.OpenAsync();
            var count = await connection.ExecuteScalarAsync<int>(sql);
            TotalObjects = count;
            
            Log.Information("Total equipment objects count updated: {TotalObjects}", TotalObjects);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load total equipment objects count");
        }
    }

    private async Task AddAsync()
    {
        Log.Debug("AddAsync started. Title={Title}, EquipmentTypeId={EquipmentTypeId}, InvNumber={InvNumber}",
            Title, SelectedEquipmentType?.Id, InvNumber);
        
        if (string.IsNullOrWhiteSpace(Title) ||
            string.IsNullOrWhiteSpace(SerialNumber) ||
            string.IsNullOrWhiteSpace(Model) ||
            string.IsNullOrWhiteSpace(InvNumber) ||
            SelectedEquipmentType is null)
        {
            Log.Warning(
                "AddAsync aborted: invalid form data. Title={Title}, EquipmentTypeId={EquipmentTypeId}, SerialNumberIsEmpty={SerialEmpty}, ModelIsEmpty={ModelEmpty}, InvNumberIsEmpty={InvEmpty}",
                Title,
                SelectedEquipmentType?.Id,
                string.IsNullOrWhiteSpace(SerialNumber),
                string.IsNullOrWhiteSpace(Model),
                string.IsNullOrWhiteSpace(InvNumber));
            return;
        }
        
        try
        {
            await using var connection = new NpgsqlConnection(AppConfig.ConnectionString());
            const string sql = "INSERT INTO public.equipment (title, user_id, equipment_type_id, serial_number, model, inv_num) VALUES (@title, @user_id, @equipment_type_id, @serial_number, @model, @inv_num)";
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);

            if (AuthService.CurrentSession is null)
            {
                Log.Warning("AddAsync aborted: CurrentSession is null, user is not authenticated.");
                return;
            }

            if (SelectedEquipmentType?.Id is null)
            {
                Log.Warning(
                    "AddAsync aborted: SelectedEquipmentType.Id is null. SelectedEquipmentTypeType={Type}",
                    SelectedEquipmentType?.Type);
                return;
            }
            
            command.Parameters.AddWithValue("@title", Title);
            command.Parameters.AddWithValue("@user_id", AuthService.CurrentSession.Id);
            command.Parameters.AddWithValue("@equipment_type_id", SelectedEquipmentType.Id);
            command.Parameters.AddWithValue("@serial_number", SerialNumber);
            command.Parameters.AddWithValue("@model", Model);
            command.Parameters.AddWithValue("@inv_num", InvNumber);
            
            Log.Debug("Executing INSERT for equipment. Title={Title}, UserId={UserId}, EquipmentTypeId={EquipmentTypeId}",
                Title, AuthService.CurrentSession.Id, SelectedEquipmentType.Id);

            var rows = await command.ExecuteNonQueryAsync();
            
            Log.Information("Equipment inserted. RowsAffected={RowsAffected}", rows);

            Clear();
            Log.Debug("Form cleared after insert");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to add equipment. Title={Title}, EquipmentTypeId={EquipmentTypeId}, InvNumber={InvNumber}",
                Title, SelectedEquipmentType?.Id, InvNumber);
            throw;
        }
    }

    private void Clear()
    {
        Log.Debug("Clear() called. Resetting form fields");

        SelectedEquipmentType = EquipmentTypes.FirstOrDefault();
        Title = string.Empty;
        SerialNumber = string.Empty;
        Model = string.Empty;
        InvNumber = string.Empty;
        
        Log.Debug("Form fields reset. SelectedEquipmentTypeId={EquipmentTypeId}",
            SelectedEquipmentType?.Id);
    }
}
