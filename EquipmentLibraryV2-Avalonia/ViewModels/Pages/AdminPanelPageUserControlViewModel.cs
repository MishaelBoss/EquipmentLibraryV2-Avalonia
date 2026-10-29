using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;
using Npgsql;
using Serilog;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using Dapper;
using EquipmentLibraryV2_Avalonia.Helpers;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Models;
using EquipmentLibraryV2_Avalonia.Services;
using Humanizer;
using Mapster;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Pages;

public partial class AdminPanelPageUserControlViewModel : ViewModelBase, IRecipient<RefreshUserListMessage>, IRecipient<RefreshDataMessage>, IDisposable
{
    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool ShowActiveUsers { get; set; } = true;
    [ObservableProperty] public partial bool ShowInactiveUsers { get; set; } = true;
    [ObservableProperty] public partial bool IsLoading { get; set; }
    [ObservableProperty] public partial ObservableCollection<CartUserViewModel> UserList { get; set; } = [];

    partial void OnSearchTextChanged(string value) => _ = ScheduleSearchAsync();
    partial void OnShowActiveUsersChanged(bool value) => _ = ScheduleSearchAsync();
    partial void OnShowInactiveUsersChanged(bool value) => _ = ScheduleSearchAsync();

    private CancellationTokenSource _searchCancellationTokenSource = new();
    private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);

    [RelayCommand]
    public void AddUser() 
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditUserMessage());
    }

    public AdminPanelPageUserControlViewModel() 
    {
        IsActive = true;
        
        TypeAdapterConfig<User, CartUserViewModel>.NewConfig()
            .Map(dest => dest.UserId, src => src.Id)
            .Map(dest => dest.DateJoined, src => src.DateJoined.Humanize(false, null, null));
        
        LoadInitialUsers();
    }

    public async void Receive(RefreshUserListMessage message)
    {
        try
        {
            await LoadUsersWithResetAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to refresh user list upon receiving message");
        }
    }

    public async void Receive(RefreshDataMessage message)
    {
        try
        {
            await LoadUsersWithResetAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to refresh data upon receiving message");
        }
    }

    private async void LoadInitialUsers()
    {
        try
        {
            await AuthService.TryAutoLoginAsync();
            await LoadUsersWithResetAsync();
        }
        catch (Exception ex)
        {
            Log.Error("Error during initial load {ExMessage}", ex.Message);
        }
    }

    private async Task ScheduleSearchAsync()
    {
        try
        {
            await _searchCancellationTokenSource.CancelAsync();
            _searchCancellationTokenSource = new CancellationTokenSource();
            var token = _searchCancellationTokenSource.Token;

            await Task.Delay(300, token);

            if (!token.IsCancellationRequested)
            {
                await LoadUsersWithResetAsync(token);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            Log.Error("Error scheduling search {ExMessage}", ex.Message);
        }
    }

    [RelayCommand]
    private async Task LoadUsersWithResetAsync(CancellationToken cancellationToken = default)
    {
        if (!await ConnectivityService.IsServerReachableAsync())
            return;

        if (!await _loadingSemaphore.WaitAsync(0, cancellationToken))
            return;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            UserList.Clear();

            var userIds = await GetFilteredUserIdsAsync(cancellationToken);

            if (userIds.Count > 0)
            {
                await LoadUserByIdsAsync(userIds, cancellationToken);
                
                var dynamicText = userIds.Count.ToQuantity("пользователь", "пользователя", "пользователей");
                Log.Information("Поиск успешно завершен. Найдено: {QuantityText}", dynamicText);
            }
        }
        catch (OperationCanceledException)
        {
            Log.Information("LoadUsersWithResetAsync cancelled");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error loading users");
            await Dispatcher.UIThread.InvokeAsync(() => IsLoading = true);
            UserList.Clear();
        }
        finally
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsLoading = false);
            _loadingSemaphore.Release();
        }
    }

    private async Task<List<long>> GetFilteredUserIdsAsync(CancellationToken cancellationToken = default)
    {
        var currentUserId = AuthService.CurrentSession?.Id;

        var userIds = new List<long>();

        try
        {
            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            await connection.OpenAsync(cancellationToken);

            var sql = "SELECT DISTINCT u.id FROM public.users u WHERE 1=1";
            var parameters = new List<NpgsqlParameter>();

            if (ShowActiveUsers && !ShowInactiveUsers)
            {
                sql += " AND u.is_active = true";
            }
            else if (!ShowActiveUsers && ShowInactiveUsers)
            {
                sql += " AND u.is_active = false";
            }

            if (!string.IsNullOrWhiteSpace(SearchText) && SearchText != "%")
            {
                sql += " AND (u.login ILIKE @search OR u.first_name ILIKE @search OR u.last_name ILIKE @search)";
                parameters.Add(new NpgsqlParameter("@search", $"%{SearchText}%"));
            }

            if (currentUserId.HasValue) 
            {
                sql += " AND u.id != @currentUserId";
                parameters.Add(new NpgsqlParameter("@currentUserId", currentUserId.Value));
            }

            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddRange(parameters.ToArray());

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                if (!reader.IsDBNull(0))
                {
                    userIds.Add(reader.GetInt64(0));
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error getting filtered user IDs {ExMessage}", ex.Message);
        }

        return userIds;
    }

    private async Task LoadUserByIdsAsync(List<long> userIds, CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0) return;

        try
        {
            const string sql = @"
                SELECT id, 
                       login, 
                       user_type_id AS UserRole, 
                       first_name AS FirstName, 
                       last_name AS LastName, 
                       middle_name AS MiddleName, 
                       is_active AS IsActive, 
                       date_joined AS DateJoined 
                FROM public.users 
                WHERE id = ANY(@ids) 
                ORDER BY last_name, first_name";
            
            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            await connection.OpenAsync(cancellationToken);
            
            var usersFromDb = await connection.QueryAsync<User>(new CommandDefinition(sql, new {ids = userIds }, cancellationToken: cancellationToken));
            var newUsers = usersFromDb.Adapt<List<CartUserViewModel>>();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                UserList.Clear();
                foreach (var user in newUsers)
                {
                    UserList.Add(user);
                }
            });
        }
        catch (NpgsqlException ex)
        {
            Log.Warning("Connection or request error {ExMessage}", ex.Message);
        }
        catch (Exception ex)
        {
            Log.Error("Error loading users by IDs {ExMessage}", ex.Message);
        }
    }
        
    [RelayCommand]
    private async Task ResetFilters()
    {
        try
        {
            ShowActiveUsers = true;
            ShowInactiveUsers = true;
            SearchText = string.Empty;

            await LoadUsersWithResetAsync();
        }
        catch (Exception ex)
        {
            Log.Error("Error resetting filters {ExMessage}", ex.Message);
        }
    }

    public void Dispose()
    {
        IsActive = false;
            
        _searchCancellationTokenSource.Cancel();
        _searchCancellationTokenSource.Dispose();
            
        _loadingSemaphore.Dispose();

        WeakReferenceMessenger.Default.UnregisterAll(this);

        GC.SuppressFinalize(this);
    }
}