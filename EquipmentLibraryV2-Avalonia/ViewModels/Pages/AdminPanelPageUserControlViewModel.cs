using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;
using Npgsql;
using Serilog;
using System.Collections.ObjectModel;
using Avalonia.Threading;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Services;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Pages
{
    public partial class AdminPanelPageUserControlViewModel : ViewModelBase, IRecipient<RefreshUserListMessage>, IRecipient<RefreshDataMessage>
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
            WeakReferenceMessenger.Default.Register<RefreshUserListMessage>(this);
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
                Log.Error(ex.ToString());
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
                Log.Error(ex.ToString());
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
                Log.Error($"Error during initial load {ex.Message}");
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
                Log.Error($"Error scheduling search {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task LoadUsersWithResetAsync(CancellationToken cancellationToken = default)
        {
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
                Log.Error($"Error getting filtered user IDs {ex.Message}");
            }

            return userIds;
        }

        private async Task LoadUserByIdsAsync(List<long> userIds, CancellationToken cancellationToken = default)
        {
            if (userIds.Count == 0)
            {
                return;
            }

            try
            {
                var parameters = new NpgsqlParameter[userIds.Count];
                var paramNames = new string[userIds.Count];

                for (var i = 0; i < userIds.Count; i++)
                {
                    paramNames[i] = $"@id{i}";
                    parameters[i] = new NpgsqlParameter(paramNames[i], userIds[i]);
                }

                var sql = $@"SELECT DISTINCT * FROM public.users WHERE id IN ({string.Join(", ", paramNames)}) ORDER BY last_name, first_name";

                await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
                await connection.OpenAsync(cancellationToken);

                await using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddRange(parameters);

                await using var reader = await command.ExecuteReaderAsync(cancellationToken);

                var newUsers = new List<CartUserViewModel>();
                var loadedUserIds = new HashSet<long>();

                while (await reader.ReadAsync(cancellationToken))
                {
                    var userId = reader.IsDBNull(0) ? 0 : reader.GetInt64(0);

                    if (userId == 0 || !loadedUserIds.Add(userId))
                        continue;

                    var userViewModel = new CartUserViewModel
                    {
                        UserId = userId,
                        UserRole = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                        Login = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        FirstName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                        LastName = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                        MiddleName = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                        Password = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                        IsActive = !reader.IsDBNull(7) && reader.GetBoolean(7),
                        DateJoined = reader.IsDBNull(8) ? string.Empty : reader.GetDateTime(8).ToString("yyyy-MM-dd"),
                    };

                    newUsers.Add(userViewModel);
                }

                UserList = new ObservableCollection<CartUserViewModel>(newUsers);
            }
            catch (NpgsqlException ex)
            {
                Log.Warning($"Connection or request error {ex.Message}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error loading users by IDs {ex.Message}");
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
                Log.Error($"Error resetting filters {ex.Message}");
            }
        }

        public void Dispose()
                => WeakReferenceMessenger.Default.UnregisterAll(this);
    }
}
