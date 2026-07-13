using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Messages;
using Npgsql;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components;

public partial class PasswordResetUserControlViewModel(long? userId = null, string? login = null) : ViewModelBase
{
    [ObservableProperty] public partial string? Login { get; set; } = login;
    [ObservableProperty] public partial string? GeneratedPassword { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsStatus { get; set; } = false;

    [RelayCommand]
    public void Close() 
    {
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditUserMessage());
    }

    [RelayCommand]
    public async Task Generate()
    {
        IsStatus = true;
        await GeneratePassword();
    }

    [RelayCommand]
    public void ConfirmAndCopyPassword()
    {
        if (GeneratedPassword != null) TextCopy.ClipboardService.SetText(GeneratedPassword);
        
        WeakReferenceMessenger.Default.Send(new OpenOrClosePasswordResetMessage());
    }

    private async Task GeneratePassword(int length = 16)
    {
        if (userId == null)
        {
            return;
        }
        
        if (length < 16) length = 16;

        var password = new StringBuilder();
        try
        {
            const string lowercase = "abcdefghijklmnopqrstuvwxyz";
            const string uppercase = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            const string digits = "0123456789";
            const string specials = "!@#$%^&*()_+-=[]{}|;:,.<>?";
            const string allChars = lowercase + uppercase + digits + specials;

            password.Append(lowercase[RandomNumberGenerator.GetInt32(lowercase.Length)]);
            password.Append(uppercase[RandomNumberGenerator.GetInt32(uppercase.Length)]);
            password.Append(digits[RandomNumberGenerator.GetInt32(digits.Length)]);
            password.Append(specials[RandomNumberGenerator.GetInt32(specials.Length)]);
            
            for (var i = 4; i < length; i++)
            {
                password.Append(allChars[RandomNumberGenerator.GetInt32(allChars.Length)]);
            }
            
            var passwordArray = password.ToString().ToCharArray();
            for (var i = passwordArray.Length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (passwordArray[i], passwordArray[j]) = (passwordArray[j], passwordArray[i]);
            }
            
            var finalPasswordStr = new string(passwordArray);
            
            const string sql = "UPDATE public.users SET password = crypt(@password, gen_salt('bf', 10)) WHERE id = @id";
            
            await using var connection = new NpgsqlConnection(await AppConfig.ConnectionAsync());
            await connection.OpenAsync();
            
            await using var command = new NpgsqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", userId);
            command.Parameters.AddWithValue("@password", finalPasswordStr);
            
            await command.ExecuteNonQueryAsync();

            GeneratedPassword = finalPasswordStr;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }
}