using EquipmentLibraryV2_Avalonia.ViewModels;
using EquipmentLibraryV2_Avalonia.ViewModels.Components;
using EquipmentLibraryV2_Avalonia.ViewModels.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace EquipmentLibraryV2_Avalonia;

public static class AppServices
{
    private static ServiceProvider? _provider;

    public static ServiceProvider Provider =>
        _provider ??= Configure().BuildServiceProvider();

    private static ServiceCollection Configure()
    {
        var services = new ServiceCollection();

        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<RightBoardUserControlViewModel>();

        services.AddTransient<AdminPanelPageUserControlViewModel>();
        services.AddTransient<LibraryPageUserControlViewModel>();
        services.AddTransient<WorkAreaUserControlViewModel>();
        services.AddTransient<MeasurementRegisterPageUserControlViewModel>();
        services.AddTransient<RegisterOfTestingEquipmentPageUserControlViewModel>();
        services.AddTransient<AuthorizationUserControlViewModel>();

        return services;
    }

    public static T Get<T>() where T : notnull => Provider.GetRequiredService<T>();
}