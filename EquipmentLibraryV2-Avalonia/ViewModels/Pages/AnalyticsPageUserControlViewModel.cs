using System.Collections.Generic;
using EquipmentLibraryV2_Avalonia.Controls;

namespace EquipmentLibraryV2_Avalonia.ViewModels.Pages;

public partial class AnalyticsPageUserControlViewModel : ViewModelBase
{
    public int SiTotal { get; set; } = 120;
    public int SdkTotal { get; set; } = 30;
    public int IoTotal { get; set; } = 55;

    public IReadOnlyList<DonutChartSegment> SiSegments { get; } = new List<DonutChartSegment>
    {
        new() { Label = "Поверено", Value = 100, Color = "#28A745" },
        new() { Label = "Не поверено", Value = 10, Color = "#E74C3C" },
        new() { Label = "Нет калибровки", Value = 10, Color = "#E67E22" },
    };

    public IReadOnlyList<DonutChartSegment> SdkSegments { get; } = new List<DonutChartSegment>
    {
        new() { Label = "Поверено", Value = 10, Color = "#28A745" },
        new() { Label = "Не поверено", Value = 10, Color = "#E74C3C" },
        new() { Label = "Нет калибровки", Value = 10, Color = "#E67E22" },
    };

    public IReadOnlyList<DonutChartSegment> IoSegments { get; } = new List<DonutChartSegment>
    {
        new() { Label = "Поверено", Value = 35, Color = "#28A745" },
        new() { Label = "Не поверено", Value = 10, Color = "#E74C3C" },
        new() { Label = "Нет калибровки", Value = 10, Color = "#E67E22" },
    };
}