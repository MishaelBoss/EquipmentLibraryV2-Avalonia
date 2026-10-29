using Avalonia;
using Avalonia.Controls;

namespace EquipmentLibraryV2_Avalonia.Views.Components;

public partial class AppTitleControlUserControlView : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<AppTitleControlUserControlView, string>(
            nameof(Title),
            defaultValue: string.Empty);

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }
    
    public AppTitleControlUserControlView()
    {
        InitializeComponent();
    }
}