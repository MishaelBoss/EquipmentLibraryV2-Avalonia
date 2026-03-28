using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Dapper;
using EquipmentLibraryV2_Avalonia.Models;
using EquipmentLibraryV2_Avalonia.Scripts;
using Npgsql;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Pages;

public partial class WorkAreaUserControlViewModel : ViewModelBase
{
    [ObservableProperty] public ObservableCollection<EquipmentType> _equipmentTypes = [];
    [ObservableProperty] private EquipmentType? _selectedEquipmentType;

    public WorkAreaUserControlViewModel() 
    {
        Task.Run(() => RefreshDataAsync());
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
            using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
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
