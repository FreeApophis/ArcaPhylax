using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace NutritionPlugin.Controls;

/// <summary>
/// A dependency-free line chart. Given a <see cref="Series"/> of <see cref="ChartPoint"/>s it draws a
/// framed, auto-scaled polyline with a marker at each point. Both axes are normalised to the control's
/// current pixel bounds, so the caller supplies raw values (e.g. day numbers and calories) without
/// pre-scaling. An empty or absent series renders just the frame.
/// </summary>
public sealed class LineChart : Control
{
    public static readonly StyledProperty<IReadOnlyList<ChartPoint>?> SeriesProperty =
        AvaloniaProperty.Register<LineChart, IReadOnlyList<ChartPoint>?>(nameof(Series));

    public static readonly StyledProperty<IBrush> StrokeProperty =
        AvaloniaProperty.Register<LineChart, IBrush>(nameof(Stroke), Brushes.SteelBlue);

    static LineChart()
    {
        AffectsRender<LineChart>(SeriesProperty, StrokeProperty);
    }

    public IReadOnlyList<ChartPoint>? Series
    {
        get => GetValue(SeriesProperty);
        set => SetValue(SeriesProperty, value);
    }

    public IBrush Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var width = Bounds.Width;
        var height = Bounds.Height;
        if (width <= 0 || height <= 0)
        {
            return;
        }

        const double padX = 6;
        const double padTop = 8;
        const double padBottom = 6;

        var framePen = new Pen(new SolidColorBrush(Colors.Gray, 0.4), 1);
        var plotLeft = padX;
        var plotRight = width - padX;
        var plotTop = padTop;
        var plotBottom = height - padBottom;

        // Baseline + left axis so an empty chart still reads as a chart.
        context.DrawLine(framePen, new Point(plotLeft, plotTop), new Point(plotLeft, plotBottom));
        context.DrawLine(framePen, new Point(plotLeft, plotBottom), new Point(plotRight, plotBottom));

        var series = Series;
        if (series is null || series.Count == 0)
        {
            return;
        }

        var (minX, maxX, minY, maxY) = Extent(series);
        var spanX = maxX - minX == 0 ? 1 : maxX - minX;
        var spanY = maxY - minY == 0 ? 1 : maxY - minY;
        var plotWidth = plotRight - plotLeft;
        var plotHeight = plotBottom - plotTop;

        Point Map(ChartPoint point) => new(
            plotLeft + ((point.X - minX) / spanX * plotWidth),
            plotBottom - ((point.Y - minY) / spanY * plotHeight));

        var linePen = new Pen(Stroke, 2)
        {
            LineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };

        for (var i = 1; i < series.Count; i++)
        {
            context.DrawLine(linePen, Map(series[i - 1]), Map(series[i]));
        }

        foreach (var point in series)
        {
            context.DrawEllipse(Stroke, null, Map(point), 2.5, 2.5);
        }
    }

    private static (double MinX, double MaxX, double MinY, double MaxY) Extent(IReadOnlyList<ChartPoint> series)
    {
        var minX = double.MaxValue;
        var maxX = double.MinValue;
        var minY = double.MaxValue;
        var maxY = double.MinValue;

        foreach (var point in series)
        {
            minX = Math.Min(minX, point.X);
            maxX = Math.Max(maxX, point.X);
            minY = Math.Min(minY, point.Y);
            maxY = Math.Max(maxY, point.Y);
        }

        return (minX, maxX, minY, maxY);
    }
}
