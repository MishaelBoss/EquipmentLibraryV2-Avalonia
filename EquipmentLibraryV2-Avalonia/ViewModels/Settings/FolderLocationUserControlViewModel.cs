using System.Diagnostics;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentLibraryV2_Avalonia.Infrastructure;
using EquipmentLibraryV2_Avalonia.Services.Interfaces;
using Serilog;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Settings;

public partial class FolderLocationUserControlViewModel : ViewModelBase, ISettingsPage
{
    private readonly AppSettings _settings;
    private string? _originalFolderDownload;
    private bool _originalUseRawFolder;
    
    [ObservableProperty] public partial string? FolderText { get; set; }
    [ObservableProperty] public partial bool IsSelectFolder { get; set; }
    [ObservableProperty] public partial bool UseRawSelectedFolder { get; set; }

    public bool HasChanges 
        => FolderText != _originalFolderDownload ||
           UseRawSelectedFolder != _originalUseRawFolder;
    
    public FolderLocationUserControlViewModel(AppSettings settings)
    {
        _settings = settings;

        if (string.IsNullOrEmpty(_settings.FolderDownload))
        {
            _settings.FolderDownload = AppPaths.DefaultDownloadDir;
        }

        _originalFolderDownload = _settings.FolderDownload;
        _originalUseRawFolder = _settings.UseRawSelectedFolder;
        
        FolderText = _settings.FolderDownload;
        UseRawSelectedFolder = _settings.UseRawSelectedFolder;
        IsSelectFolder = !string.IsNullOrEmpty(FolderText);
    }
    
    [RelayCommand]
    public void OpenDownloadFolder()
    {
        var downloadsPath = !string.IsNullOrEmpty(FolderText) 
            ? FolderText 
            : Path.Combine(AppPaths.DefaultDownloadDir);

        if (!Directory.Exists(downloadsPath))
        {
            Directory.CreateDirectory(downloadsPath);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = downloadsPath,
            UseShellExecute = true,
            Verb = "open"
        });
    }

    [RelayCommand]
    private async Task SelectFolder(CancellationToken cancellationToken)
    {
        try
        {
            var baseFolder = await SelectFolderAsync();
            if (!string.IsNullOrEmpty(baseFolder))
            {
                var fullPath = Path.Combine(baseFolder, AppPaths.CustomDownloadDir);
                FolderText = fullPath;
                IsSelectFolder = !string.IsNullOrEmpty(fullPath);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to select folder");
        }
    }
    
    private static async Task<string?> SelectFolderAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");

        var foldesr = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "Select Folder",
            AllowMultiple = false
        });

        return foldesr is {Count: > 0} ? foldesr[0].Path.LocalPath : null;
    }

    partial void OnUseRawSelectedFolderChanged(bool value)
    {
        if (string.IsNullOrEmpty(FolderText)) return;

        FolderText = value switch
        {
            true when FolderText.EndsWith(AppPaths.CustomDownloadDir, StringComparison.OrdinalIgnoreCase) 
                => FolderText[..^AppPaths.CustomDownloadDir.Length].TrimEnd(Path.DirectorySeparatorChar),
            false when !FolderText.EndsWith(AppPaths.CustomDownloadDir, StringComparison.OrdinalIgnoreCase) 
                => Path.Combine(FolderText, AppPaths.CustomDownloadDir),
            _ => FolderText
        };
    }

    public void Save()
    {
        _settings.FolderDownload = FolderText;
        _settings.UseRawSelectedFolder = UseRawSelectedFolder;
        
        _originalFolderDownload = FolderText;
        _originalUseRawFolder = UseRawSelectedFolder;
    }
}