using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Dapper;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;
using Npgsql;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Pages;

public partial class LibraryPageUserControlViewModel : ViewModelBase, IRecipient<RefreshDataMessage>
{
    [ObservableProperty] public partial string? SearchText { get; set; }
    [ObservableProperty] public partial EquipmentType? SelectedEquipmentType { get; set; }
    [ObservableProperty] public partial ObservableCollection<EquipmentItem> EquipmentItems { get; set; } = [];
    [ObservableProperty] public partial ObservableCollection<EquipmentType> EquipmentTypes { get; set; } = [];
    [ObservableProperty] public partial bool IsLoading { get; set; }
    
    public bool IsEmpty => EquipmentItems.Count == 0 && !IsLoading;

    partial void OnIsLoadingChanged(bool value) => OnPropertyChanged(nameof(IsEmpty));

    private CancellationTokenSource _debounceCts = new();
    
    partial void OnEquipmentItemsChanged(ObservableCollection<EquipmentItem> value)
    {
        value.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsEmpty));
    }

    partial void OnSearchTextChanged(string? value) => _ = DebounceFilterChanged();
    partial void OnSelectedEquipmentTypeChanged(EquipmentType? value) => _ = DebounceFilterChanged();
    
    public async void Receive(RefreshDataMessage message)
    {
        try
        {
            await RefreshDataAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex.ToString());
        }
    }

    public LibraryPageUserControlViewModel()
    {
        Log.Information("LibraryPageUserControlViewModel created");

        Task.Run(async () =>
        {
            Log.Debug("Initial data refresh started");
            await RefreshDataAsync();
            Log.Debug("Initial data refresh finished");
        });
    }

    private async Task DebounceFilterChanged()
    {
        await _debounceCts.CancelAsync();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;
        try
        {
            await Task.Delay(300, token);
            Log.Debug("Filter changed, reloading data");
            if (!token.IsCancellationRequested)
                await LoadAllEquipmentsAsync();
        }
        catch (TaskCanceledException)
        {
        }
    }

    private async Task RefreshDataAsync()
    {
        await LoadEquipmentTypesAsync();
        await LoadAllEquipmentsAsync();
    }

    private async Task LoadEquipmentTypesAsync()
    {
        try
        {
            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            const string sql = "SELECT id, type FROM public.equipment_type ORDER BY type";

            var data = (await connection.QueryAsync<EquipmentType>(sql)).ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                EquipmentTypes.Clear();
                EquipmentTypes.Add(new EquipmentType(0, "Все"));
                SelectedEquipmentType = EquipmentTypes.FirstOrDefault();
                foreach (var item in data)
                {
                    EquipmentTypes.Add(item);
                }
            });

            Log.Information("Loaded {Count} equipment types", data.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load equipment types");
        }
    }

    private async Task LoadAllEquipmentsAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => IsLoading = true);

        try
        {
            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());

            var sql = @"SELECT e.id, e.title, e.serial_number AS SerialNumber, e.model, e.inv_num AS InvNum,
                               e.equipment_type_id AS EquipmentTypeId, et.type AS EquipmentTypeName,
                               e.user_id AS UserId, u.login AS UserLogin
                        FROM public.equipment e
                        LEFT JOIN public.equipment_type et ON e.equipment_type_id = et.id
                        LEFT JOIN public.users u ON e.user_id = u.id
                        WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                sql += " AND (e.title ILIKE @Search OR e.serial_number ILIKE @Search OR e.model ILIKE @Search OR e.inv_num ILIKE @Search)";
            }

            if (SelectedEquipmentType is not null && SelectedEquipmentType.Id != 0)
            {
                sql += " AND e.equipment_type_id = @TypeId";
            }

            sql += " ORDER BY e.id DESC";

            var data = (await connection.QueryAsync<EquipmentItem>(sql, new
            {
                Search = $"%{SearchText}%",
                TypeId = SelectedEquipmentType?.Id
            })).ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                EquipmentItems.Clear();
                foreach (var item in data)
                {
                    EquipmentItems.Add(item);
                }
            });

            Log.Information("Loaded {Count} equipment items", data.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to load equipment items");
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsLoading = false);
        }
    }

    public void Dispose() 
        => WeakReferenceMessenger.Default.UnregisterAll(this);
}
