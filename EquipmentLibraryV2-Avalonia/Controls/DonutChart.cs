using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace EquipmentLibraryV2_Avalonia.Controls;

public class DonutChartSegment
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Color { get; set; } = "#28A745";

    public IBrush ColorBrush => Brush.Parse(Color);
}

public class DonutChart : Control
{
    public static readonly StyledProperty<IEnumerable<DonutChartSegment>?> ItemsSourceProperty =
        AvaloniaProperty.Register<DonutChart, IEnumerable<DonutChartSegment>?>(nameof(ItemsSource));

    public static readonly StyledProperty<double> RingThicknessProperty =
        AvaloniaProperty.Register<DonutChart, double>(nameof(RingThickness), 18.0);

    public IEnumerable<DonutChartSegment>? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public double RingThickness
    {
        get => GetValue(RingThicknessProperty);
        set => SetValue(RingThicknessProperty, value);
    }

    public DonutChart()
    {
        IsHitTestVisible = false;
        PropertyChanged += (_, e) =>
        {
            if (e.Property == ItemsSourceProperty || e.Property == RingThicknessProperty)
                InvalidateVisual();
        };
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var segments = ItemsSource?.Where(s => s.Value > 0).ToList();
        if (segments is null || segments.Count == 0)
            return;

        var total = segments.Sum(s => s.Value);
        if (total <= 0)
            return;

        var size = Math.Min(Bounds.Width, Bounds.Height);
        if (size <= 0)
            return;

        var center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        var outer = size / 2 - 1;
        var inner = outer - RingThickness;
        if (inner < 1)
            return;

        var start = -90.0;
        foreach (var segment in segments)
        {
            var sweep = segment.Value / total * 360.0;
            context.DrawGeometry(segment.ColorBrush, null, BuildSegmentGeometry(center, outer, inner, start, sweep));
            start += sweep;
        }
    }

    private static Geometry BuildSegmentGeometry(Point c, double outer, double inner, double startAngle, double sweep)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            var outerEnd = PointAt(c, outer, startAngle + sweep);
            var innerEnd = PointAt(c, inner, startAngle + sweep);
            var innerStart = PointAt(c, inner, startAngle);

            ctx.BeginFigure(PointAt(c, outer, startAngle), true);
            AppendArc(ctx, c, outer, startAngle, startAngle + sweep, SweepDirection.Clockwise);
            ctx.LineTo(innerEnd, true);
            AppendArc(ctx, c, inner, startAngle + sweep, startAngle, SweepDirection.CounterClockwise);
            ctx.LineTo(innerStart, true);
            ctx.EndFigure(true);
        }
        return geometry;
    }

    private static void AppendArc(
        StreamGeometryContext ctx,
        Point center,
        double radius,
        double fromAngle,
        double toAngle,
        SweepDirection direction)
    {
        var total = Math.Abs(toAngle - fromAngle);
        if (total < 0.001)
            return;

        var size = new Size(radius, radius);
        var current = fromAngle;

        while (total > 0.001)
        {
            var step = Math.Min(180.0, total);
            var next = direction == SweepDirection.Clockwise ? current + step : current - step;
            ctx.ArcTo(PointAt(center, radius, next), size, 0, step > 180.0, direction, true);
            current = next;
            total -= step;
        }
    }

    private static Point PointAt(Point c, double radius, double angleDeg)
    {
        var rad = angleDeg * Math.PI / 180.0;
        return new Point(c.X + radius * Math.Sin(rad), c.Y - radius * Math.Cos(rad));
    }
}