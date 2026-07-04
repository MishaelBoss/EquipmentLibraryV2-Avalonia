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
using EquipmentLibraryV2_Avalonia.Infrastructure;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Pages;

public partial class WorkAreaUserControlViewModel : ViewModelBase
{
    [ObservableProperty] public partial ObservableCollection<EquipmentType> EquipmentTypes { get; set; } = [];
    [ObservableProperty] public partial EquipmentType? SelectedEquipmentType { get; set; }

    public WorkAreaUserControlViewModel() 
    {
        Task.Run(async () => await RefreshDataAsync());
    }

    private async Task RefreshDataAsync() 
    {
        Dispatcher.UIThread.Post(async () => {
            EquipmentTypes.Clear();
        });

        await LoadEquipmentTypeAsync();
    }

    private async Task LoadEquipmentTypeAsync() 
    {
        try
        {
            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            const string sql = "SELECT id, type FROM public.equipment_type";
            var data = (await connection.QueryAsync<EquipmentType>(sql)).ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var item in data)
                {
                    EquipmentTypes.Add(item);
                }
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex.Message);
        }
    }
}
