using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dapper;
using EquipmentLibraryV2_Avalonia.Models;
using EquipmentLibraryV2_Avalonia.Messages;
using Npgsql;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using EquipmentLibraryV2_Avalonia.Infrastructure;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components
{
    public partial class AddOrEditUserUserControlViewModel : ViewModelBase
    {
        private readonly long? _id;
        private readonly int? _initialUserRoleId;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))]
        public partial string? Login { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))]
        public partial string? FirstName { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))]
        public partial string? LastName { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))]
        public partial string? Password { get; set; }

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsActiveConfirmButton))]
        public partial UserRole? SelectUserRole { get; set; }

        [ObservableProperty] public partial ObservableCollection<UserRole> UserRoles { get; set; } = [];

        public bool IsActiveConfirmButton
            => !string.IsNullOrEmpty(Login)
            && !string.IsNullOrEmpty(FirstName)
            && !string.IsNullOrEmpty(LastName)
            && !string.IsNullOrEmpty(Password)
            && SelectUserRole != null;

        public AddOrEditUserUserControlViewModel(long? id = null, string? login = null, string? firstName = null, string? lastName = null, string? password = null, int? userRole = null)
        {
            _id = id ?? 0;
            Login = login ?? string.Empty;
            FirstName = firstName ?? string.Empty;
            LastName = lastName ?? string.Empty;
            Password = password ?? string.Empty;
            _initialUserRoleId = userRole ?? 0;

            try
            {
                _ = LoadUserRoles();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
        }

        [RelayCommand]
        public void Close() 
        {
            WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditUserMessage());
        }

        [RelayCommand]
        public async Task Confirm() 
        {
            if (string.IsNullOrEmpty(Login) || string.IsNullOrEmpty(FirstName) || string.IsNullOrEmpty(LastName) || string.IsNullOrEmpty(Password) || SelectUserRole == null)
            {
                return;
            }

            try
            {
                const string addUserSql = "INSERT INTO public.users (login, first_name, last_name, password, user_type_id, date_joined) " +
                                    "VALUES (@login, @first_name, @last_name, @password, @user_type_id, now())";

                const string updateUserSql = @"UPDATE public.users SET login = @login, first_name = @first_name, last_name = @last_name, password = @password, user_type_id = @user_type_id WHERE id = @id";

                var sql = _id == 0 ? addUserSql : updateUserSql;

                await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
                await connection.OpenAsync();
                await using var command = new NpgsqlCommand(sql, connection);

                command.Parameters.AddWithValue("@login", Login!);
                command.Parameters.AddWithValue("@first_name", FirstName!);
                command.Parameters.AddWithValue("@last_name", LastName!);
                command.Parameters.AddWithValue("@password", Password!);
                command.Parameters.AddWithValue("@user_type_id", SelectUserRole!.Id);
                if (_id != 0) command.Parameters.AddWithValue("@id", _id!);

                await command.ExecuteNonQueryAsync();

                WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditUserMessage());
                WeakReferenceMessenger.Default.Send(new RefreshUserListMessage());

                ClearForm();
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private async Task LoadUserRoles() 
        {
            const string sql = "SELECT * FROM public.user_type";
            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            var result = await connection.QueryAsync<UserRole>(sql);

            UserRoles.Clear();

            foreach (var role in result)
            {
                UserRoles.Add(role);
            }

            if (_initialUserRoleId.HasValue)
            {
                SelectUserRole = UserRoles.FirstOrDefault(r => r.Id == _initialUserRoleId.Value);
            }
        }

        private void ClearForm()
        {
            Login = string.Empty;
            FirstName = string.Empty;
            LastName = string.Empty;
            Password = string.Empty;
        }
    }
}
