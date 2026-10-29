using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using EquipmentLibraryV2_Avalonia.Models;
using Npgsql;
using Serilog;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Services;
using EquipmentLibraryV2_Avalonia.Validators.Pages;
using Mapster;

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

        var validator = new WorkAreaValidator();
        var result = await validator.ValidateAsync(this);

        if (!result.IsValid)
        {
            var firstError = result.Errors.FirstOrDefault()?.ErrorMessage ?? "Ошибка валидации";
            Log.Warning("Валидация формы не пройдена: {Error}", firstError);
            
            return;
        }

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
        
        Title = string.Empty;
        SerialNumber = string.Empty;
        Model = string.Empty;
        InvNumber = string.Empty;

        TypeAdapterConfig<WorkAreaUserControlViewModel, EquipmentDb>.NewConfig()
            .Ignore(dest => dest.EquipmentTypeId) 
            .Map(dest => dest.InvNum, src => src.InvNumber)
            .Map(dest => dest.EquipmentTypeId, src => src.SelectedEquipmentType != null ? src.SelectedEquipmentType.Id : 0); 
        
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
        if (!await ConnectivityService.IsServerReachableAsync())
            return;

        Log.Debug("LoadEquipmentTypeAsync started");
        
        try
        {
            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            const string sql = "SELECT id, type FROM public.equipment_type";
            
            Log.Debug("Executing SQL to load equipment types: {Sql}", sql);
            
            var dbResult = await connection.QueryAsync<EquipmentType>(sql);
            var result = dbResult.Adapt<List<EquipmentType>>();

            Log.Information("Loaded {Count} equipment types from database", result.Count);
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var item in result)
                {
                    EquipmentTypes.Add(item);
                }

                SelectedEquipmentType ??= EquipmentTypes.FirstOrDefault();

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
        if (!await ConnectivityService.IsServerReachableAsync())
            return;

        Log.Debug("LoadAllObjectsAsync started");
        
        try
        {
            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            const string sql = "SELECT COUNT(*) FROM public.equipment";
            
            Log.Debug("Executing SQL to count equipment: {Sql}", sql);
            await connection.OpenAsync();
            var count = await connection.ExecuteScalarAsync<int>(sql);
            await Dispatcher.UIThread.InvokeAsync(() => TotalObjects = count);
            
            Log.Information("Total equipment objects count updated: {TotalObjects}", TotalObjects);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load total equipment objects count");
        }
    }

    private async Task AddAsync()
    {
        Log.Debug("Add Async started. Title={Title}, EquipmentTypeId={EquipmentTypeId}, InvNumber={InvNumber}",
            Title, SelectedEquipmentType?.Id, InvNumber);
        
        if (AuthService.CurrentSession is null || SelectedEquipmentType?.Id is null)
        {
            Log.Warning("Add Async aborted: User is not authenticated or type is missing");
            return;
        }
        
        try
        {
            var equipmentDto = new EquipmentDb
            {
                Title = Title,
                Model = Model,
                SerialNumber = SerialNumber,
                InvNum = InvNumber,
                EquipmentTypeId = SelectedEquipmentType.Id, 
                UserId = AuthService.CurrentSession.Id
            };

            const string sql = @"INSERT INTO public.equipment (title, user_id, equipment_type_id, serial_number, model, inv_num) 
                VALUES (@Title, @UserId, @EquipmentTypeId, @SerialNumber, @Model, @InvNum)";
            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            await connection.OpenAsync();
            
            Log.Debug("Executing Dapper INSERT for equipment. Title={Title}, UserId={UserId}", equipmentDto.Title, equipmentDto.UserId);
            
            var rows = await connection.ExecuteAsync(sql, equipmentDto);

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
