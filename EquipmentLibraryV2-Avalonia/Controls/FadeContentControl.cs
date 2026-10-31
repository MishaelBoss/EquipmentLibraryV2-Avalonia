using System;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Media.Transformation;

namespace EquipmentLibraryV2_Avalonia;

public class FadeContentControl : ContentControl
{
    private static readonly TransformOperations HiddenTransform =
        TransformOperations.Parse("translateY(8px)");

    private static readonly TransformOperations ShownTransform =
        TransformOperations.Parse("translateY(0px)");

    private readonly DoubleTransition _opacityTransition = new()
    {
        Property = OpacityProperty,
        Duration = TimeSpan.FromMilliseconds(180),
        Easing = new CubicEaseOut()
    };

    private readonly TransformOperationsTransition _transformTransition = new()
    {
        Property = RenderTransformProperty,
        Duration = TimeSpan.FromMilliseconds(180),
        Easing = new CubicEaseOut()
    };

    private bool _first = true;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != ContentProperty)
            return;

        if (_first)
        {
            _first = false;
            Opacity = 1;
            return;
        }

        // Значения выставляются без transition, а затем transition включаются обратно,
        // чтобы анимация шла от начального состояния к конечному, а не наоборот.
        var transitions = Transitions;
        Transitions = null;

        Opacity = 0;
        RenderTransform = HiddenTransform;

        Transitions = transitions;
        EnsureTransitions();

        Opacity = 1;
        RenderTransform = ShownTransform;
    }

    private void EnsureTransitions()
    {
        Transitions ??= new Transitions();

        if (!Transitions.Contains(_opacityTransition))
            Transitions.Add(_opacityTransition);

        if (!Transitions.Contains(_transformTransition))
            Transitions.Add(_transformTransition);
    }
}
