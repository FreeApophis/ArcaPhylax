using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using ComputeSharp;
using FractalPlugin.Shaders;

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
    private string? _error;
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
            var ft = new FormattedText(
                _error ?? "Rendering…",
                System.Globalization.CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                Typeface.Default,
                14,
                _error is null ? Brushes.White : Brushes.OrangeRed);

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

        // ComputeSharp's Rgba32 stores bytes in R,G,B,A order, so use a matching bitmap format
        // and the readback becomes a straight row-by-row copy.
        WriteableBitmap bmp = new(
            new PixelSize(w, h),
            new Vector(96, 96),
            PixelFormat.Rgba8888,
            AlphaFormat.Unpremul);

        try
        {
            await Task.Run(() => RenderMandelbrot(bmp, cx, cy, scale, maxIt, token), token);
            if (!token.IsCancellationRequested)
            {
                _bmp = bmp;
                _error = null;
                // Ask Avalonia to redraw with the finished bitmap
                await Dispatcher.UIThread.InvokeAsync(InvalidateVisual);
            }
        }
        catch (OperationCanceledException)
        {
            // ignored
        }
        catch (Exception ex)
        {
            _bmp = null;
            _error = $"GPU render failed: {ex.Message}";
            await Dispatcher.UIThread.InvokeAsync(InvalidateVisual);
        }
    }

    /// <summary>
    /// Dispatches the Mandelbrot compute shader on the default GPU, then copies the resulting
    /// texture back into the bitmap's framebuffer.
    /// </summary>
    private static unsafe void RenderMandelbrot(
        WriteableBitmap bmp,
        double centerX,
        double centerY,
        double scale,
        int maxIterations,
        CancellationToken token)
    {
        int w = bmp.PixelSize.Width;
        int h = bmp.PixelSize.Height;

        GraphicsDevice device = GraphicsDevice.GetDefault();

        using ReadWriteTexture2D<Rgba32, float4> texture =
            device.AllocateReadWriteTexture2D<Rgba32, float4>(w, h);

        device.For(w, h, new MandelbrotShader(
            texture,
            (float)centerX,
            (float)centerY,
            (float)scale,
            maxIterations));

        token.ThrowIfCancellationRequested();

        Rgba32[] pixels = new Rgba32[w * h];
        texture.CopyTo(pixels);

        token.ThrowIfCancellationRequested();

        using ILockedFramebuffer fb = bmp.Lock();
        byte* basePtr = (byte*)fb.Address;
        int stride = fb.RowBytes;
        int rowBytes = w * 4;

        fixed (Rgba32* src = pixels)
        {
            byte* srcBytes = (byte*)src;
            for (int y = 0; y < h; y++)
            {
                Buffer.MemoryCopy(srcBytes + y * rowBytes, basePtr + y * stride, rowBytes, rowBytes);
            }
        }
    }

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
