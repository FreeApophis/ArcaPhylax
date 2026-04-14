using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;

namespace FractalPlugin.Controls;

public sealed class MandelbrotView : Control
{
    // ---- Public (bindable) properties ----

    public static readonly StyledProperty<double> CenterXProperty =
        AvaloniaProperty.Register<MandelbrotView, double>(nameof(CenterX), -0.5);

    public static readonly StyledProperty<double> CenterYProperty =
        AvaloniaProperty.Register<MandelbrotView, double>(nameof(CenterY), 0.0);

    /// <summary>
    /// Complex-plane units per pixel. Smaller = more zoomed in.
    /// </summary>
    public static readonly StyledProperty<double> ScaleProperty =
        AvaloniaProperty.Register<MandelbrotView, double>(nameof(Scale), 0.005);

    public static readonly StyledProperty<int> MaxIterationsProperty =
        AvaloniaProperty.Register<MandelbrotView, int>(nameof(MaxIterations), 500);

    public double CenterX
    {
        get => GetValue(CenterXProperty);
        set => SetValue(CenterXProperty, value);
    }

    public double CenterY
    {
        get => GetValue(CenterYProperty);
        set => SetValue(CenterYProperty, value);
    }

    public double Scale
    {
        get => GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public int MaxIterations
    {
        get => GetValue(MaxIterationsProperty);
        set => SetValue(MaxIterationsProperty, value);
    }

    // ---- Rendering state ----

    private WriteableBitmap? _bmp;
    private CancellationTokenSource? _cts;
    private readonly DispatcherTimer _debounce;

    // Interaction state
    private bool _panning;
    private Point _panStart;
    private double _startCx, _startCy;

    static MandelbrotView()
    {
        AffectsRender<MandelbrotView>(CenterXProperty, CenterYProperty, ScaleProperty, MaxIterationsProperty);
        // If you want *only* bitmap redraw (not immediate render invalidate), we will also hook changes below.
    }

    public MandelbrotView()
    {
        ClipToBounds = true;

        _debounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
        _debounce.Tick += (_, _) =>
        {
            _debounce.Stop();
            _ = RebuildBitmapAsync();
        };

        // Re-render when size changes
        //this.GetObservable(BoundsProperty).Subscribe(_ => RequestRebuild());

        // Rebuild when fractal params change
        //this.GetObservable(CenterXProperty).Subscribe(_ => RequestRebuild());
        //this.GetObservable(CenterYProperty).Subscribe(_ => RequestRebuild());
        //this.GetObservable(ScaleProperty).Subscribe(_ => RequestRebuild());
        //this.GetObservable(MaxIterationsProperty).Subscribe(_ => RequestRebuild());

        // Input
        AddHandler(PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Tunnel);
        AddHandler(PointerMovedEvent, OnPointerMoved, RoutingStrategies.Tunnel);
        AddHandler(PointerWheelChangedEvent, OnPointerWheelChanged, RoutingStrategies.Tunnel);

        RequestRebuild();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var rect = new Rect(Bounds.Size);

        // Background
        context.FillRectangle(Brushes.Black, rect);

        if (_bmp is not null)
        {
            context.DrawImage(_bmp, new Rect(_bmp.Size), new Rect(Bounds.Size));
        }
        else
        {
            // Simple placeholder text (optional)
            var ft = new FormattedText(
                "Rendering…",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                14,
                Brushes.White);

            context.DrawText(ft, new Point(10, 10));
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == BoundsProperty ||
            change.Property == CenterXProperty ||
            change.Property == CenterYProperty ||
            change.Property == ScaleProperty ||
            change.Property == MaxIterationsProperty)
        {
            RequestRebuild();
        }
    }

    private void RequestRebuild()
    {
        // debounce rapid changes (wheel, drag, resize)
        _debounce.Stop();
        _debounce.Start();
    }

    private async Task RebuildBitmapAsync()
    {
        var w = (int)Math.Max(1, Math.Floor(Bounds.Width));
        var h = (int)Math.Max(1, Math.Floor(Bounds.Height));
        if (w <= 1 || h <= 1) return;

        // Cancel prior render
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        // Snapshot parameters to keep a consistent frame
        var cx = CenterX;
        var cy = CenterY;
        var scale = Scale;
        var maxIt = Math.Max(1, MaxIterations);

        // Allocate bitmap on UI thread (safe)
        WriteableBitmap bmp = new(
            new PixelSize(w, h),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Unpremul);

        _bmp = bmp;
        InvalidateVisual();

        try
        {
            await Task.Run(() => RenderMandelbrot(bmp, cx, cy, scale, maxIt, token), token);
            if (!token.IsCancellationRequested)
            {
                // Ask Avalonia to redraw with the finished bitmap
                await Dispatcher.UIThread.InvokeAsync(InvalidateVisual);
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
    }

    private unsafe void RenderMandelbrot(
        WriteableBitmap bmp,
        double centerX,
        double centerY,
        double scale,
        int maxIterations,
        CancellationToken token)
    {
        using var fb = bmp.Lock();

        int w = fb.Size.Width;
        int h = fb.Size.Height;
        int stride = fb.RowBytes;

        byte* basePtr = (byte*)fb.Address;

        // Map pixel (x,y) -> complex plane:
        // re = centerX + (x - w/2)*scale
        // im = centerY + (y - h/2)*scale
        double halfW = w * 0.5;
        double halfH = h * 0.5;

        // Simple coloring: smooth-ish gradient using normalized iteration count.
        for (int y = 0; y < h; y++)
        {
            token.ThrowIfCancellationRequested();
            byte* row = basePtr + y * stride;

            double ci = centerY + (y - halfH) * scale;

            for (int x = 0; x < w; x++)
            {
                double cr = centerX + (x - halfW) * scale;

                // Mandelbrot iteration: z_{n+1} = z_n^2 + c
                double zr = 0.0, zi = 0.0;
                double zr2 = 0.0, zi2 = 0.0;

                int it = 0;
                while (it < maxIterations && (zr2 + zi2) <= 4.0)
                {
                    zi = 2.0 * zr * zi + ci;
                    zr = zr2 - zi2 + cr;

                    zr2 = zr * zr;
                    zi2 = zi * zi;
                    it++;
                }

                // Write BGRA
                int idx = x * 4;

                if (it >= maxIterations)
                {
                    // inside set: black
                    row[idx + 0] = 0;   // B
                    row[idx + 1] = 0;   // G
                    row[idx + 2] = 0;   // R
                    row[idx + 3] = 255; // A
                }
                else
                {
                    // Color based on iterations (simple, fast palette)
                    // t in [0,1)
                    double t = it / (double)maxIterations;

                    // A quick gradient without allocating palettes:
                    // Use a few sine-ish curves approximated by polynomials for speed.
                    // (You can replace with your own palette.)
                    byte r = (byte)(255.0 * Clamp01(1.0 - Math.Pow(1.0 - t, 3)));
                    byte g = (byte)(255.0 * Clamp01(1.0 - Math.Pow(1.0 - t, 2)));
                    byte b = (byte)(255.0 * Clamp01(Math.Sqrt(t)));

                    row[idx + 0] = b;
                    row[idx + 1] = g;
                    row[idx + 2] = r;
                    row[idx + 3] = 255;
                }
            }
        }
    }

    private static double Clamp01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);

    // ---- Interaction ----

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _panning = true;
            _panStart = e.GetPosition(this);
            _startCx = CenterX;
            _startCy = CenterY;
            e.Pointer.Capture(this);
            e.Handled = true;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_panning)
        {
            _panning = false;
            e.Pointer.Capture(null);
            e.Handled = true;
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_panning) return;

        var p = e.GetPosition(this);
        var dx = p.X - _panStart.X;
        var dy = p.Y - _panStart.Y;

        // Dragging right should move view right (i.e. centerX decreases because pixels map to +re)
        CenterX = _startCx - dx * Scale;
        CenterY = _startCy - dy * Scale;

        RequestRebuild();
        e.Handled = true;
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        var delta = e.Delta.Y;
        if (Math.Abs(delta) < 0.01) return;

        var pos = e.GetPosition(this);

        int w = (int)Math.Max(1, Math.Floor(Bounds.Width));
        int h = (int)Math.Max(1, Math.Floor(Bounds.Height));
        if (w <= 1 || h <= 1) return;

        // Complex coordinate under cursor before zoom
        double halfW = w * 0.5;
        double halfH = h * 0.5;

        double beforeRe = CenterX + (pos.X - halfW) * Scale;
        double beforeIm = CenterY + (pos.Y - halfH) * Scale;

        // Zoom factor
        // wheel up -> zoom in -> scale smaller
        double zoom = Math.Pow(0.85, delta);
        double newScale = Scale * zoom;

        // Clamp scale to avoid going to zero/infinite
        newScale = Math.Clamp(newScale, 1e-12, 1.0);

        Scale = newScale;

        // Complex coordinate under cursor after zoom, adjust center to keep same point under cursor
        double afterRe = CenterX + (pos.X - halfW) * Scale;
        double afterIm = CenterY + (pos.Y - halfH) * Scale;

        CenterX += (beforeRe - afterRe);
        CenterY += (beforeIm - afterIm);

        RequestRebuild();
        e.Handled = true;
    }
}
