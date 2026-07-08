using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EquipmentLibraryV2_Avalonia.Models;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace EquipmentLibraryV2_Avalonia.ViewModels;

public partial class AboutDialogWindowViewModel : ViewModelBase
{
    [ObservableProperty] public partial List<PackageInfo> Packages { get; set; } = [];

    [RelayCommand]
    public void Close(Window? window)
    {
        window?.Close();
    }

    public AboutDialogWindowViewModel() {
        Dispatcher.UIThread.Post(GetPakedgesInProject);
    }

    private void GetPakedgesInProject() {
        var list = new List<PackageInfo>();

        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null)
        {
            string appName = entryAssembly.GetName().Name ?? "Приложение";
            string appVersion = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split("+")[0]
                                ?? entryAssembly.GetName().Version?.ToString(3)
                                ?? "unknown";
            list.Add(new PackageInfo(appName, appVersion));
        }

        var referencedNames = entryAssembly?.GetReferencedAssemblies() ?? Array.Empty<AssemblyName>();

        foreach (var assemblyName in referencedNames)
        {
            try
            {
                var assembly = Assembly.Load(assemblyName);

                var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split("+")[0]
                                 ?? assemblyName.Version?.ToString(3)
                                 ?? "unknown";

                var name = assemblyName.Name ?? "unknown";
                if (!name.StartsWith("System.") && !name.StartsWith("Microsoft.") && name != "netstandard")
                {
                    list.Add(new PackageInfo(name, version));
                }
            }
            catch
            {
                list.Add(new PackageInfo(assemblyName.Name ?? "unknown", assemblyName.Version?.ToString(3) ?? "unknown"));
            }
        }

        Packages = list;
    }
}