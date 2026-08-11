using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using TwoDCore.Core.Audio;
using TwoDCore.Core.Scene;
using TwoDCore.Editor.ViewModels;

namespace TwoDCore.Editor.Controls;

public sealed class SceneViewport : Control
{
    private const double HandleSize = 8;
    private readonly EditorSession _session;
    private readonly Dictionary<string, Bitmap> _textures = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<SceneObject, ScenePoint> _dragStarts = [];
    private Point _cameraOffset;
    private Point _cameraStart;
    private Point _pointerStart;
    private ScenePoint _objectStart;
    private SceneSize _sizeStart;
    private ScenePoint _marqueeStart;
    private ScenePoint _marqueeEnd;
    private double _zoom = 1;
    private bool _cameraInitialized;
    private bool _panning;
    private bool _dragging;
    private bool _resizing;
    private bool _marqueeSelecting;
    private bool _marqueeAdditive;
    private ResizeHandle _resizeHandle;

    public SceneViewport(EditorSession session)
    {
        _session = session;
        Focusable = true;
        ClipToBounds = true;
        _session.SceneChanged += (_, _) => InvalidateVisual();
        PointerPressed += OnPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerWheelChanged += OnPointerWheelChanged;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (!_cameraInitialized && finalSize.Width > 0 && finalSize.Height > 0)
        {
            _cameraOffset = new Point(finalSize.Width / 2, finalSize.Height / 2);
            _cameraInitialized = true;
        }
        return base.ArrangeOverride(finalSize);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#121317")), Bounds);
        DrawGrid(context);

        foreach (SceneObject item in DrawableObjects()) DrawObject(context, item);

        if (_marqueeSelecting)
        {
            Rect marquee = WorldSelectionRect(_marqueeStart, _marqueeEnd);
            Point topLeft = WorldToScreen(new ScenePoint(marquee.X, marquee.Y));
            Rect screen = new(topLeft, new Size(marquee.Width * _zoom, marquee.Height * _zoom));
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(40, 52, 120, 246)), screen);
            context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#3478F6")), 1, dashStyle: DashStyle.Dash), screen);
        }
    }

    private IEnumerable<SceneObject> DrawableObjects() => SceneGraph
        .Traverse(_session.Scene.Objects.Where(item => item is not SoundServiceObject))
        .Where(item => item is not FolderObject);

    private void DrawGrid(DrawingContext context)
    {
        const double grid = 40;
        ScenePoint topLeft = ScreenToWorld(new Point(0, 0));
        ScenePoint bottomRight = ScreenToWorld(new Point(Bounds.Width, Bounds.Height));
        double startX = Math.Floor(topLeft.X / grid) * grid;
        double startY = Math.Floor(topLeft.Y / grid) * grid;
        Pen minor = new(new SolidColorBrush(Color.Parse("#1B1D24")), 1);
        Pen major = new(new SolidColorBrush(Color.Parse("#292C36")), 1);

        for (double x = startX; x <= bottomRight.X; x += grid)
        {
            double screenX = WorldToScreen(new ScenePoint(x, 0)).X;
            context.DrawLine(Math.Abs(x % (grid * 4)) < 0.01 ? major : minor, new Point(screenX, 0), new Point(screenX, Bounds.Height));
        }
        for (double y = startY; y <= bottomRight.Y; y += grid)
        {
            double screenY = WorldToScreen(new ScenePoint(0, y)).Y;
            context.DrawLine(Math.Abs(y % (grid * 4)) < 0.01 ? major : minor, new Point(0, screenY), new Point(Bounds.Width, screenY));
        }

        Point origin = WorldToScreen(new ScenePoint(0, 0));
        context.DrawLine(new Pen(Brushes.Red, 1.5), new Point(0, origin.Y), new Point(Bounds.Width, origin.Y));
        context.DrawLine(new Pen(Brushes.LimeGreen, 1.5), new Point(origin.X, 0), new Point(origin.X, Bounds.Height));
    }

    private void DrawObject(DrawingContext context, SceneObject item)
    {
        Rect rect = WorldRectToScreen(item);
        double opacity = Math.Clamp(1 - item.Transparency, 0, 1);

        if (item is SpatialSoundObject spatial)
        {
            Point center = WorldToScreen(new ScenePoint(item.Position.X + item.Size.Width / 2, item.Position.Y + item.Size.Height / 2));
            bool selected = _session.IsSelected(item);
            Pen radiusPen = new(new SolidColorBrush(selected ? Color.Parse("#00DC41") : Color.Parse("#00B932")), 2);
            context.DrawEllipse(null, radiusPen, center, spatial.Radius * _zoom, spatial.Radius * _zoom);
            if (selected && spatial.FullVolumeRadius > 0)
            {
                Pen innerPen = new(new SolidColorBrush(Color.Parse("#50DC6E")), 1, dashStyle: DashStyle.Dash);
                context.DrawEllipse(null, innerPen, center, spatial.FullVolumeRadius * _zoom, spatial.FullVolumeRadius * _zoom);
            }
        }

        if (item is SoundObjectBase) DrawSoundIcon(context, rect, opacity);
        else if (item is ImageObject && TryGetTexture(item.TexturePath, out Bitmap texture))
        {
            using (context.PushOpacity(opacity)) context.DrawImage(texture, new Rect(texture.Size), rect);
        }
        else
        {
            using (context.PushOpacity(opacity)) context.FillRectangle(new SolidColorBrush(ParseColor(item.ColorHex, Colors.White)), rect);
        }

        if (!_session.IsSelected(item)) return;
        context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#3478F6")), 2), rect);
        if (_session.SelectedObjects.Count == 1) DrawHandles(context, rect);
    }

    private static void DrawSoundIcon(DrawingContext context, Rect rect, double opacity)
    {
        using (context.PushOpacity(opacity))
        {
            context.FillRectangle(new SolidColorBrush(Color.Parse("#282A36")), rect);
            Point center = rect.Center;
            StreamGeometry speaker = new();
            using (StreamGeometryContext geometry = speaker.Open())
            {
                geometry.BeginFigure(new Point(center.X - 13, center.Y - 7), true);
                geometry.LineTo(new Point(center.X - 5, center.Y - 7));
                geometry.LineTo(new Point(center.X + 5, center.Y - 15));
                geometry.LineTo(new Point(center.X + 5, center.Y + 15));
                geometry.LineTo(new Point(center.X - 5, center.Y + 7));
                geometry.LineTo(new Point(center.X - 13, center.Y + 7));
                geometry.EndFigure(true);
            }
            context.DrawGeometry(Brushes.White, null, speaker);
            context.DrawEllipse(null, new Pen(Brushes.White, 2), new Point(center.X + 5, center.Y), 10, 10);
        }
    }

    private static void DrawHandles(DrawingContext context, Rect rect)
    {
        foreach (Point point in HandlePoints(rect).Select(pair => pair.Point))
        {
            Rect handle = new(point.X - HandleSize / 2, point.Y - HandleSize / 2, HandleSize, HandleSize);
            context.FillRectangle(Brushes.White, handle);
            context.DrawRectangle(null, new Pen(Brushes.Black, 1), handle);
        }
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Focus();
        PointerPoint point = e.GetCurrentPoint(this);
        _pointerStart = point.Position;
        if (point.Properties.IsMiddleButtonPressed)
        {
            _panning = true;
            _cameraStart = _cameraOffset;
            Cursor = new Cursor(StandardCursorType.SizeAll);
            e.Pointer.Capture(this);
            return;
        }
        if (!point.Properties.IsLeftButtonPressed) return;

        bool control = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        if (_session.SelectedObjects.Count == 1 && _session.SelectedObject is { } selected)
        {
            ResizeHandle handle = HitTestHandle(selected, point.Position);
            if (handle != ResizeHandle.None)
            {
                _session.Capture();
                _resizeHandle = handle;
                _objectStart = selected.Position;
                _sizeStart = selected.Size;
                _resizing = true;
                e.Pointer.Capture(this);
                return;
            }
        }

        ScenePoint world = ScreenToWorld(point.Position);
        SceneObject? hit = DrawableObjects().Reverse().FirstOrDefault(item => Contains(item, world));
        if (hit != null)
        {
            if (control) _session.Select(hit, additive: true, toggle: true);
            else if (!_session.IsSelected(hit)) _session.Select(hit);
            if (!_session.IsSelected(hit)) return;

            _session.Capture();
            _dragStarts.Clear();
            foreach (SceneObject item in _session.SelectedObjects) _dragStarts[item] = item.Position;
            _dragging = true;
            e.Pointer.Capture(this);
            return;
        }

        if (!control) _session.ClearSelection();
        _marqueeSelecting = true;
        _marqueeAdditive = control;
        _marqueeStart = world;
        _marqueeEnd = world;
        e.Pointer.Capture(this);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        Point current = e.GetPosition(this);
        if (_panning)
        {
            _cameraOffset = _cameraStart + (current - _pointerStart);
            InvalidateVisual();
            return;
        }
        if (_marqueeSelecting)
        {
            _marqueeEnd = ScreenToWorld(current);
            InvalidateVisual();
            return;
        }
        if (_resizing && _session.SelectedObject is { } resizingItem)
        {
            ResizeSelected(resizingItem, (current - _pointerStart) / _zoom);
            InvalidateVisual();
            return;
        }
        if (_dragging)
        {
            Vector delta = (current - _pointerStart) / _zoom;
            foreach ((SceneObject item, ScenePoint start) in _dragStarts)
                item.Position = new(start.X + delta.X, start.Y + delta.Y);
            InvalidateVisual();
            return;
        }

        ResizeHandle hover = _session.SelectedObjects.Count == 1 && _session.SelectedObject is { } selected
            ? HitTestHandle(selected, current)
            : ResizeHandle.None;
        Cursor = new Cursor(CursorFor(hover));
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_marqueeSelecting)
        {
            Rect selection = WorldSelectionRect(_marqueeStart, _marqueeEnd);
            IEnumerable<SceneObject> hits = DrawableObjects().Where(item => selection.Intersects(new Rect(item.Position.X, item.Position.Y, item.Size.Width, item.Size.Height)));
            if (_marqueeAdditive) hits = _session.SelectedObjects.Concat(hits);
            _session.SetSelection(hits);
        }

        bool moved = _dragging;
        bool resized = _resizing;
        _panning = false;
        _dragging = false;
        _resizing = false;
        _marqueeSelecting = false;
        _resizeHandle = ResizeHandle.None;
        Cursor = new Cursor(StandardCursorType.Arrow);
        e.Pointer.Capture(null);
        if (moved) _session.CommitPropertyChange("Moved object");
        else if (resized) _session.CommitPropertyChange("Resized object");
        InvalidateVisual();
    }

    private void ResizeSelected(SceneObject item, Vector delta)
    {
        double left = _objectStart.X;
        double top = _objectStart.Y;
        double right = _objectStart.X + _sizeStart.Width;
        double bottom = _objectStart.Y + _sizeStart.Height;
        const double minimum = 15;

        if (_resizeHandle is ResizeHandle.Left or ResizeHandle.TopLeft or ResizeHandle.BottomLeft) left = Math.Min(left + delta.X, right - minimum);
        if (_resizeHandle is ResizeHandle.Right or ResizeHandle.TopRight or ResizeHandle.BottomRight) right = Math.Max(right + delta.X, left + minimum);
        if (_resizeHandle is ResizeHandle.Top or ResizeHandle.TopLeft or ResizeHandle.TopRight) top = Math.Min(top + delta.Y, bottom - minimum);
        if (_resizeHandle is ResizeHandle.Bottom or ResizeHandle.BottomLeft or ResizeHandle.BottomRight) bottom = Math.Max(bottom + delta.Y, top + minimum);

        item.Position = new(left, top);
        item.Size = new(right - left, bottom - top);
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        _zoom = Math.Clamp(e.Delta.Y > 0 ? _zoom * 1.15 : _zoom / 1.15, 0.1, 5);
        InvalidateVisual();
        e.Handled = true;
    }

    public void ZoomIn() => SetZoom(_zoom * 1.15);
    public void ZoomOut() => SetZoom(_zoom / 1.15);

    private void SetZoom(double value)
    {
        _zoom = Math.Clamp(value, 0.1, 5);
        InvalidateVisual();
    }

    private Point WorldToScreen(ScenePoint point) => new(point.X * _zoom + _cameraOffset.X, point.Y * _zoom + _cameraOffset.Y);
    private ScenePoint ScreenToWorld(Point point) => new((point.X - _cameraOffset.X) / _zoom, (point.Y - _cameraOffset.Y) / _zoom);

    private Rect WorldRectToScreen(SceneObject item)
    {
        Point point = WorldToScreen(item.Position);
        return new Rect(point, new Size(item.Size.Width * _zoom, item.Size.Height * _zoom));
    }

    private static Rect WorldSelectionRect(ScenePoint first, ScenePoint second) => new(
        Math.Min(first.X, second.X), Math.Min(first.Y, second.Y), Math.Abs(first.X - second.X), Math.Abs(first.Y - second.Y));

    private static bool Contains(SceneObject item, ScenePoint point) =>
        point.X >= item.Position.X && point.X <= item.Position.X + item.Size.Width &&
        point.Y >= item.Position.Y && point.Y <= item.Position.Y + item.Size.Height;

    private ResizeHandle HitTestHandle(SceneObject item, Point pointer)
    {
        foreach ((ResizeHandle handle, Point point) in HandlePoints(WorldRectToScreen(item)))
        {
            if (new Rect(point.X - 7, point.Y - 7, 14, 14).Contains(pointer)) return handle;
        }
        return ResizeHandle.None;
    }

    private static IEnumerable<(ResizeHandle Handle, Point Point)> HandlePoints(Rect rect)
    {
        yield return (ResizeHandle.TopLeft, rect.TopLeft);
        yield return (ResizeHandle.Top, new Point(rect.Center.X, rect.Top));
        yield return (ResizeHandle.TopRight, rect.TopRight);
        yield return (ResizeHandle.Right, new Point(rect.Right, rect.Center.Y));
        yield return (ResizeHandle.BottomRight, rect.BottomRight);
        yield return (ResizeHandle.Bottom, new Point(rect.Center.X, rect.Bottom));
        yield return (ResizeHandle.BottomLeft, rect.BottomLeft);
        yield return (ResizeHandle.Left, new Point(rect.Left, rect.Center.Y));
    }

    private static StandardCursorType CursorFor(ResizeHandle handle) => handle switch
    {
        ResizeHandle.TopLeft or ResizeHandle.BottomRight => StandardCursorType.TopLeftCorner,
        ResizeHandle.TopRight or ResizeHandle.BottomLeft => StandardCursorType.TopRightCorner,
        ResizeHandle.Top or ResizeHandle.Bottom => StandardCursorType.SizeNorthSouth,
        ResizeHandle.Left or ResizeHandle.Right => StandardCursorType.SizeWestEast,
        _ => StandardCursorType.Arrow
    };

    private bool TryGetTexture(string path, out Bitmap bitmap)
    {
        bitmap = null!;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return false;
        if (_textures.TryGetValue(path, out Bitmap? cached) && cached != null)
        {
            bitmap = cached;
            return true;
        }
        try
        {
            bitmap = new Bitmap(path);
            _textures[path] = bitmap;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Color ParseColor(string value, Color fallback)
    {
        try { return Color.Parse(value); }
        catch { return fallback; }
    }

    private enum ResizeHandle
    {
        None,
        TopLeft,
        Top,
        TopRight,
        Right,
        BottomRight,
        Bottom,
        BottomLeft,
        Left
    }
}
