using System.Net.Sockets;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Messages;
using EquipmentLibraryV2_Avalonia.Models;
using EquipmentLibraryV2_Avalonia.Services.Interfaces;
using EquipmentLibraryV2_Avalonia.Services;
using EquipmentLibraryV2_Avalonia.Validators.Settings;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;
using Npgsql;
using Serilog;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Settings;

public partial class ConnectionUserControlViewModel: ViewModelBase, ISettingsPage
{
    private readonly AppSettings _settings;
    private string _originalIp;
    private string _originalPort;
    private string _originalDatabase;
    private string _originalUser;
    private string _originalPassword;
    
    [ObservableProperty] public partial string Ip { get; set; }
    [ObservableProperty] public partial string Port { get; set; }
    [ObservableProperty] public partial string Database { get; set; }
    [ObservableProperty] public partial string Password { get; set; }
    [ObservableProperty] public partial string User { get; set; }
    [ObservableProperty] public partial ConnectionTestStatusType TestStatus { get; set; } = ConnectionTestStatusType.Idle;
    [ObservableProperty] public partial bool IsTesting { get; set; }

    private static readonly IBrush StatusTestingBrush = new SolidColorBrush(Color.Parse("#F0A500"));
    private static readonly IBrush StatusSuccessBrush = new SolidColorBrush(Color.Parse("#28A745"));
    private static readonly IBrush StatusFailureBrush = new SolidColorBrush(Color.Parse("#E74C3C"));

    public bool StatusVisible => TestStatus != ConnectionTestStatusType.Idle;

    public string StatusText => TestStatus switch
    {
        ConnectionTestStatusType.Testing => "Проверка подключения...",
        ConnectionTestStatusType.Success => "Подключение к базе данных успешно",
        ConnectionTestStatusType.Failure => "Не удалось подключиться.",
        _ => string.Empty
    };

    public IBrush StatusBrush => TestStatus switch
    {
        ConnectionTestStatusType.Testing => StatusTestingBrush,
        ConnectionTestStatusType.Success => StatusSuccessBrush,
        ConnectionTestStatusType.Failure => StatusFailureBrush,
        _ => Brushes.Transparent
    };

    partial void OnTestStatusChanged(ConnectionTestStatusType value)
    {
        OnPropertyChanged(nameof(StatusVisible));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(StatusBrush));
    }

    public ConnectionUserControlViewModel(AppSettings settings)
    {
        _settings = settings;

        _originalIp = _settings.Ip;
        _originalPort = _settings.Port;
        _originalDatabase = _settings.Database;
        _originalUser = _settings.User;
        _originalPassword = _settings.Password;

        Ip = _settings.Ip;
        Port = _settings.Port;
        Database = _settings.Database;
        Password = _settings.Password;
        User = _settings.User;
    }

    [RelayCommand]
    public async Task TestConnection()
    {
        if (IsTesting)
        {
            return;
        }

        TestStatus = ConnectionTestStatusType.Testing;
        IsTesting = true;

        var ok = await ConnectivityChecker();

        if (ok)
        {
            var connString = $"Server={Ip};Port={Port};Database={Database};" +
                             $"User Id={User};Password={Password};" +
                             $"SslMode=Prefer;Timeout=10;CommandTimeout=30";
            MigrationRunner.Run(connString);
        }

        TestStatus = ok ? ConnectionTestStatusType.Success : ConnectionTestStatusType.Failure;
        IsTesting = false;

        Log.Information(ok ? "Successful connect" : "Not connection to database");
    }

    #region Test

    private bool IsConfigInvalid() =>
        string.IsNullOrWhiteSpace(Ip) ||
        string.IsNullOrWhiteSpace(Port) ||
        string.IsNullOrWhiteSpace(Database) ||
        string.IsNullOrWhiteSpace(User) ||
        string.IsNullOrWhiteSpace(Password);

    public async Task<bool> ConnectivityChecker()
    {
        try
        {
            var validator = new ConnectionValidator();
            var validationResult = await validator.ValidateAsync(this);

            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.First().ErrorMessage;
                Log.Error("Валидация настроек подключения не пройдена: {Error}", firstError);

                WeakReferenceMessenger.Default.Send(new ShowOrHideNotification(ErrorAction.Add, ErrorUserControlViewModel.Instance, (firstError, 400L)));
                return false;
            }

            if (!int.TryParse(Port, out var port)) port = 5432;
            if (await IsTcpReachableAsync(Ip, port)) return await TestPostgreSqlConnection();
            WeakReferenceMessenger.Default.Send(new ShowOrHideNotification(ErrorAction.Add, ErrorUserControlViewModel.Instance, ("PostgreSQL server is not running. Start PostgreSQL to create the database", 503L)));
            return false;

        }
        catch (Exception ex)
        {
            Log.Warning("Connection check failed: {ExMessage}", ex.Message);
            return false;
        }
    }

    private static async Task<bool> IsTcpReachableAsync(string host, int port)
    {
        using var tcp = new TcpClient();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            await tcp.ConnectAsync(host, port, cts.Token);
            Log.Information($"TCP connect to ({host},{port}) succeeded");
            return tcp.Connected;
        }
        catch (OperationCanceledException)
        {
            Log.Warning($"TCP connect to ({host},{port}) timed out after 3s");
            return false;
        }
        catch (Exception ex)
        {
            Log.Warning($"TCP connect to ({host},{port}) failed: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> TestPostgreSqlConnection()
    {
        var connString = $"Server={Ip};Port={Port};Database={Database};" +
                                  $"User Id={User};Password={Password};" +
                                  $"Timeout=10;CommandTimeout=10;Pooling=true;MaxPoolSize=5;SslMode=Prefer";
        
        try
        {
            await using var connection = new NpgsqlConnection(connString);
            await connection.OpenAsync();

            await using var cmd = new NpgsqlCommand("SELECT 1", connection);
            var result = await cmd.ExecuteScalarAsync();

            Log.Information($"PostgreSQL connection test successful, result {result}");
            return true;
        }
        catch (Exception ex)
        {
            Log.Warning($"PostgreSQL connection failed: {ex.Message}");
            WeakReferenceMessenger.Default.Send(new ShowOrHideNotification(ErrorAction.Add, ErrorUserControlViewModel.Instance, ("PostgreSQL connection failed", 504L)));
            return false;
        }
    }

    #endregion

    public bool HasChanges =>
        Ip != _originalIp ||
        Port != _originalPort ||
        Database != _originalDatabase ||
        User != _originalUser ||
        Password != _originalPassword;

    public void Save()
    {
        _settings.Ip = Ip;
        _settings.Port = Port;
        _settings.Database = Database;
        _settings.User = User;
        _settings.Password = Password;

        AppConfig.ResetConnection();

        _originalIp = Ip;
        _originalPort = Port;
        _originalDatabase = Database;
        _originalUser = User;
        _originalPassword = Password;
    }
}