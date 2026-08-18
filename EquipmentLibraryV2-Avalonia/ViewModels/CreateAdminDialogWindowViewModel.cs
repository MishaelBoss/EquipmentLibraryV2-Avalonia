using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using Npgsql;
using Serilog;
using Dapper;

namespace EquipmentLibraryV2_Avalonia.ViewModels;

public partial class CreateAdminDialogWindowViewModel : ViewModelBase
{
    [ObservableProperty] public partial string Login { get; set; } = "admin";
    [ObservableProperty] public partial string Password { get; set; } = string.Empty;
    [ObservableProperty] public partial string ConfirmPassword { get; set; } = string.Empty;
    [ObservableProperty] public partial string StatusText { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsSuccess { get; set; }
    [ObservableProperty] public partial bool IsBusy { get; set; }

    [RelayCommand]
    public async Task Create(Window? window)
    {
        StatusText = string.Empty;

        if (string.IsNullOrWhiteSpace(Login))
        {
            StatusText = "Укажите логин администратора";
            return;
        }

        if (string.IsNullOrEmpty(Password))
        {
            StatusText = "Укажите пароль";
            return;
        }

        if (Password != ConfirmPassword)
        {
            StatusText = "Пароли не совпадают";
            return;
        }

        if (Password.Length < 6)
        {
            StatusText = "Пароль должен быть не короче 6 символов";
            return;
        }

        IsBusy = true;
        try
        {
            var connectionString = await AppConfig.ConnectionAsync();
            if (string.IsNullOrEmpty(connectionString))
            {
                StatusText = "Нет подключения к базе данных";
                return;
            }

            await using var connection = new NpgsqlConnection(connectionString);

            var existing = await connection.ExecuteScalarAsync<long>(
                "SELECT COUNT(*) FROM public.users WHERE login = @login", new { login = Login });

            if (existing > 0)
            {
                StatusText = "Пользователь с таким логином уже существует";
                return;
            }

            await connection.ExecuteAsync(
                "INSERT INTO public.users (login, first_name, last_name, password, user_type_id, date_joined) " +
                "VALUES (@login, @login, '', crypt(@password, gen_salt('bf', 10)), 1, now())",
                new { login = Login, password = Password });

            Log.Information("Administrator account created: {Login}", Login);
            IsSuccess = true;
            StatusText = "Администратор создан";

            window?.Close();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to create administrator account");
            StatusText = "Не удалось создать администратора: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void Close(Window? window)
    {
        window?.Close();
    }
}