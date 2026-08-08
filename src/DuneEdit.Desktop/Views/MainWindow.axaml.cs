using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using System.Globalization;
using Avalonia.VisualTree;

namespace DuneEdit.Desktop.Views;

public partial class MainWindow : Window
{
    private const double MapWidth = 1000;
    private const double MapHeight = 620;
    private const int MapTileCopies = 5;
    private const int CenterMapTile = MapTileCopies / 2;
    private const double MinimumMapZoom = 1.0;
    private const double MaximumMapZoom = 4.0;
    private const double MouseWheelZoomFactor = 1.15;
    private const double ZoomButtonStep = 0.25;
    private const double WheelPanDistance = 50.0;

    private readonly MatrixTransform mapTransform = new();
    private double mapZoom = MinimumMapZoom;
    private Vector mapTranslation;
    private IPointer? dragPointer;
    private Point dragStart;
    private Vector dragStartTranslation;
    private bool isPinching;
    private double pinchStartZoom;

    public MainWindow()
    {
        InitializeComponent();

        MapSurface.RenderTransform = mapTransform;
        ApplyMapTransform();

        MapViewport.PointerPressed += MapPointerPressed;
        MapViewport.PointerMoved += MapPointerMoved;
        MapViewport.PointerReleased += MapPointerReleased;
        MapViewport.PointerCaptureLost += MapPointerCaptureLost;
        MapViewport.PointerWheelChanged += MapPointerWheelChanged;
        MapViewport.PointerTouchPadGestureMagnify += MapTouchPadMagnified;
        MapViewport.Pinch += MapPinched;
        MapViewport.PinchEnded += MapPinchEnded;
        MapViewport.ScrollGesture += MapScrolled;
        MapViewport.SizeChanged += MapViewportSizeChanged;
    }

    private void MapPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsOverButton(e.Source)
            || !e.GetCurrentPoint(MapViewport).Properties.IsLeftButtonPressed)
        {
            return;
        }

        dragPointer = e.Pointer;
        dragStart = e.GetPosition(MapViewport);
        dragStartTranslation = mapTranslation;
        e.Pointer.Capture(MapViewport);
        e.Handled = true;
    }

    private void MapPointerMoved(object? sender, PointerEventArgs e)
    {
        if (dragPointer != e.Pointer)
        {
            return;
        }

        var delta = e.GetPosition(MapViewport) - dragStart;
        SetMapTranslation(dragStartTranslation + delta);
        e.Handled = true;
    }

    private void MapPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (dragPointer != e.Pointer)
        {
            return;
        }

        dragPointer = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    private void MapPointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (dragPointer == e.Pointer)
        {
            dragPointer = null;
        }
    }

    private void MapPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if ((e.KeyModifiers & (KeyModifiers.Control | KeyModifiers.Meta)) != 0)
        {
            if (e.Delta.Y != 0)
            {
                var zoomFactor = Math.Pow(MouseWheelZoomFactor, e.Delta.Y);
                SetMapZoom(mapZoom * zoomFactor, e.GetPosition(MapViewport));
            }

            e.Handled = true;
            return;
        }

        var delta = e.Delta;
        if ((e.KeyModifiers & KeyModifiers.Shift) != 0 && delta.X == 0)
        {
            delta = new Vector(delta.Y, 0);
        }

        if (SetMapTranslation(mapTranslation + (delta * WheelPanDistance)))
        {
            e.Handled = true;
        }
    }

    private void MapTouchPadMagnified(object? sender, PointerDeltaEventArgs e)
    {
        SetMapZoom(
            mapZoom * Math.Exp(e.Delta.Y),
            e.GetPosition(MapViewport));
        e.Handled = true;
    }

    private void MapPinched(object? sender, PinchEventArgs e)
    {
        if (!isPinching)
        {
            isPinching = true;
            pinchStartZoom = mapZoom;
        }

        SetMapZoom(pinchStartZoom * e.Scale, e.ScaleOrigin);
        e.Handled = true;
    }

    private void MapScrolled(object? sender, ScrollGestureEventArgs e)
    {
        if (SetMapTranslation(mapTranslation - e.Delta))
        {
            e.Handled = true;
        }
    }

    private void MapPinchEnded(object? sender, PinchEndedEventArgs e)
    {
        isPinching = false;
        e.Handled = true;
    }

    private void MapViewportSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        Dispatcher.UIThread.Post(() =>
        {
            mapTranslation = ClampMapTranslation(mapTranslation);
            ApplyMapTransform();
        }, DispatcherPriority.Render);
    }

    private void SetMapZoom(double requestedZoom, Point origin)
    {
        var nextZoom = Math.Clamp(requestedZoom, MinimumMapZoom, MaximumMapZoom);
        if (Math.Abs(nextZoom - mapZoom) < double.Epsilon)
        {
            return;
        }

        var fitScale = HorizontalFitScale;
        var currentScale = fitScale * mapZoom;
        var contentPoint = new Point(
            (origin.X + (MapWidth * currentScale) - mapTranslation.X) / currentScale,
            (origin.Y - VerticalPadding - mapTranslation.Y) / currentScale);

        mapZoom = nextZoom;
        var nextScale = fitScale * mapZoom;
        mapTranslation = ClampMapTranslation(
            new Vector(
                origin.X + (MapWidth * nextScale) - (contentPoint.X * nextScale),
                origin.Y - VerticalPadding - (contentPoint.Y * nextScale)));
        ApplyMapTransform();
    }

    private bool SetMapTranslation(Vector requestedTranslation)
    {
        var nextTranslation = ClampMapTranslation(requestedTranslation);
        if (nextTranslation == mapTranslation)
        {
            return false;
        }

        mapTranslation = nextTranslation;
        ApplyMapTransform();
        return true;
    }

    private double HorizontalFitScale => MapViewport.Bounds.Width / MapWidth;

    private double VerticalPadding => Math.Max(
        0,
        (MapViewport.Bounds.Height - (MapHeight * HorizontalFitScale)) / 2);

    private Vector ClampMapTranslation(Vector translation)
    {
        var tileWidth = MapWidth * HorizontalFitScale * mapZoom;
        var minimumY = -(MapHeight * HorizontalFitScale * (mapZoom - 1));

        return new Vector(
            WrapHorizontalTranslation(translation.X, tileWidth),
            Math.Clamp(translation.Y, minimumY, 0));
    }

    private static double WrapHorizontalTranslation(double translation, double tileWidth)
    {
        if (tileWidth <= 0)
        {
            return 0;
        }

        var wrapped = translation % tileWidth;
        var halfTile = tileWidth / 2;
        return wrapped > halfTile
            ? wrapped - tileWidth
            : wrapped <= -halfTile
            ? wrapped + tileWidth
            : wrapped;
    }

    private void ApplyMapTransform()
    {
        var scale = HorizontalFitScale * mapZoom;
        mapTransform.Matrix = new Matrix(
            scale,
            0,
            0,
            scale,
            -(CenterMapTile * MapWidth * scale) + mapTranslation.X,
            VerticalPadding + mapTranslation.Y);
        MapZoomText.Text = $"{mapZoom * 100:0}%";
    }

    private void ZoomOutClicked(object? sender, RoutedEventArgs e) =>
        SetMapZoom(mapZoom - ZoomButtonStep, MapViewport.Bounds.Center);

    private void ZoomInClicked(object? sender, RoutedEventArgs e) =>
        SetMapZoom(mapZoom + ZoomButtonStep, MapViewport.Bounds.Center);

    private void MapZoomPresetClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string value }
            && double.TryParse(value, CultureInfo.InvariantCulture, out var zoom))
        {
            SetMapZoom(zoom, MapViewport.Bounds.Center);
        }
    }

    private static bool IsOverButton(object? source)
    {
        for (var visual = source as Visual; visual is not null; visual = visual.GetVisualParent())
        {
            if (visual is Button)
            {
                return true;
            }
        }

        return false;
    }
}