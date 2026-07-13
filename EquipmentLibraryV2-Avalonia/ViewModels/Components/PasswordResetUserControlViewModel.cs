using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Dapper;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Messages;
using Npgsql;
using Serilog;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Components;

public partial class PasswordResetUserControlViewModel(long? userId = null, string? login = null) : ViewModelBase
{
    [ObservableProperty] public partial string? Login { get; set; } = login;
    [ObservableProperty] public partial string? GeneratedPassword { get; set; } = string.Empty;
    [ObservableProperty] public partial bool IsStatus { get; set; } = false;

    [RelayCommand]
    public void Close() 
    {
        Log.Debug("Password reset dialog closed without changes. UserId={UserId}, Login={Login}", userId, Login);
        WeakReferenceMessenger.Default.Send(new OpenOrCloseAddOrEditUserMessage());
    }

    [RelayCommand]
    public async Task Generate()
    {
        if (userId is null)
        {
            Log.Warning("Generate password aborted: UserId is null. Login={Login}", Login);
            return;
        }
        
        Log.Information("Password generation started. UserId={UserId}, Login={Login}", userId, Login);
        
        try
        {
            await GeneratePassword();
            Log.Information("Password generation and update completed successfully. UserId={UserId}, Login={Login}", userId, Login);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Password generation failed. UserId={UserId}, Login={Login}", userId, Login);
            throw;
        }
        finally
        {
            IsStatus = true;
        }
    }

    [RelayCommand]
    public void CloseAndCopyPassword()
    {
        if (!string.IsNullOrEmpty(GeneratedPassword))
        {
            TextCopy.ClipboardService.SetText(GeneratedPassword);
            Log.Information("Password copied to clipboard and dialog closed. UserId={UserId}, Login={Login}", userId, Login);
        }
        else
        {
            Log.Warning("CloseAndCopyPassword executed with empty GeneratedPassword. UserId={UserId}, Login={Login}", userId, Login);
        }
        
        WeakReferenceMessenger.Default.Send(new OpenOrClosePasswordResetMessage());
    }

    [RelayCommand]
    public void CopyPassword()
    {
        if (!string.IsNullOrEmpty(GeneratedPassword))
        {
            TextCopy.ClipboardService.SetText(GeneratedPassword);
            Log.Information("Password copied to clipboard. UserId={UserId}, Login={Login}", userId, Login);
        }
        else
        {
            Log.Warning("CopyPassword executed with empty GeneratedPassword. UserId={UserId}, Login={Login}", userId, Login);
        }
    }

    private async Task GeneratePassword(int length = 16)
    {
        if (userId is null)
        {
            Log.Warning("GeneratePassword aborted: UserId is null.");
            return;
        }
        
        if (length < 16)
        {
            Log.Debug("Requested password length {Length} is less than minimum, using 16.", length);
            length = 16;
        }

        var password = new StringBuilder();
        try
        {
            Log.Debug("Generating secure password. UserId={UserId}, Length={Length}", userId, length);
            
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
            
            Log.Debug("Updating user password in database using pgcrypto crypt(). UserId={UserId}", userId);
            
            var rows = await connection.ExecuteAsync(sql, new { Id = userId, Password = finalPasswordStr });
            
            Log.Information("Password hash updated in database. UserId={UserId}, RowsAffected={RowsAffected}",
                userId,
                rows);

            GeneratedPassword = finalPasswordStr;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception while generating or updating password. UserId={UserId}", userId);
            throw;
        }
    }
}