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
    private readonly EditorSession _session;
    private readonly Dictionary<string, Bitmap> _textures = new(StringComparer.OrdinalIgnoreCase);
    private Point _cameraOffset = new(480, 300);
    private double _zoom = 1;
    private bool _panning;
    private bool _dragging;
    private bool _resizing;
    private ResizeHandle _resizeHandle;
    private Point _pointerStart;
    private Point _cameraStart;
    private ScenePoint _objectStart;
    private SceneSize _sizeStart;

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

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#121317")), Bounds);
        DrawGrid(context);

        foreach (SceneObject item in SceneGraph.Traverse(_session.Scene.Objects.Where(item => item is not SoundServiceObject)))
        {
            if (item is FolderObject) continue;
            DrawObject(context, item);
        }
    }

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
        context.DrawLine(new Pen(Brushes.IndianRed, 1.5), new Point(0, origin.Y), new Point(Bounds.Width, origin.Y));
        context.DrawLine(new Pen(Brushes.MediumSeaGreen, 1.5), new Point(origin.X, 0), new Point(origin.X, Bounds.Height));
    }

    private void DrawObject(DrawingContext context, SceneObject item)
    {
        Rect rect = WorldRectToScreen(item);
        double opacity = Math.Clamp(1 - item.Transparency, 0, 1);

        if (item is SpatialSoundObject spatial)
        {
            Point center = WorldToScreen(new ScenePoint(
                item.Position.X + item.Size.Width / 2,
                item.Position.Y + item.Size.Height / 2));
            Pen radiusPen = new(new SolidColorBrush(_session.SelectedObject == item ? Color.Parse("#00DC41") : Color.Parse("#009F38")), 2);
            context.DrawEllipse(null, radiusPen, center, spatial.Radius * _zoom, spatial.Radius * _zoom);
            if (_session.SelectedObject == item && spatial.FullVolumeRadius > 0)
            {
                Pen innerPen = new(new SolidColorBrush(Color.Parse("#65C97A")), 1, dashStyle: DashStyle.Dash);
                context.DrawEllipse(null, innerPen, center, spatial.FullVolumeRadius * _zoom, spatial.FullVolumeRadius * _zoom);
            }
        }

        if (item is SoundObjectBase)
        {
            DrawSoundIcon(context, rect, opacity);
        }
        else if (item is ImageObject && TryGetTexture(item.TexturePath, out Bitmap texture))
        {
            using (context.PushOpacity(opacity))
            {
                context.DrawImage(texture, new Rect(texture.Size), rect);
            }
        }
        else
        {
            Color color = ParseColor(item.ColorHex, Colors.White);
            using (context.PushOpacity(opacity)) context.FillRectangle(new SolidColorBrush(color), rect);
        }

        if (_session.SelectedObject == item)
        {
            context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#3478F6")), 2), rect);
            DrawHandles(context, rect);
        }
    }

    private static void DrawSoundIcon(DrawingContext context, Rect rect, double opacity)
    {
        using (context.PushOpacity(opacity))
        {
            context.FillRectangle(new SolidColorBrush(Color.Parse("#282A36")), rect, 4);
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
        foreach (Point point in new[] { rect.TopLeft, rect.TopRight, rect.BottomLeft, rect.BottomRight })
        {
            Rect handle = new(point.X - 4, point.Y - 4, 8, 8);
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
            e.Pointer.Capture(this);
            return;
        }
        if (!point.Properties.IsLeftButtonPressed) return;

        if (_session.SelectedObject is { } selected)
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
        SceneObject? hit = SceneGraph.Traverse(_session.Scene.Objects.Where(item => item is not SoundServiceObject))
            .Where(item => item is not FolderObject)
            .Reverse()
            .FirstOrDefault(item => Contains(item, world));
        _session.Select(hit);
        if (hit != null)
        {
            _session.Capture();
            _objectStart = hit.Position;
            _dragging = true;
            e.Pointer.Capture(this);
        }
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        Point current = e.GetPosition(this);
        if (_panning)
        {
            Vector delta = current - _pointerStart;
            _cameraOffset = _cameraStart + delta;
            InvalidateVisual();
        }
        else if (_resizing && _session.SelectedObject is { } resizingItem)
        {
            Vector delta = (current - _pointerStart) / _zoom;
            double x = _objectStart.X;
            double y = _objectStart.Y;
            double width = _sizeStart.Width;
            double height = _sizeStart.Height;
            const double minimum = 15;

            if (_resizeHandle is ResizeHandle.TopLeft or ResizeHandle.BottomLeft)
            {
                double applied = Math.Min(delta.X, _sizeStart.Width - minimum);
                x += applied;
                width -= applied;
            }
            else
            {
                width = Math.Max(minimum, _sizeStart.Width + delta.X);
            }

            if (_resizeHandle is ResizeHandle.TopLeft or ResizeHandle.TopRight)
            {
                double applied = Math.Min(delta.Y, _sizeStart.Height - minimum);
                y += applied;
                height -= applied;
            }
            else
            {
                height = Math.Max(minimum, _sizeStart.Height + delta.Y);
            }

            resizingItem.Position = new(x, y);
            resizingItem.Size = new(width, height);
            InvalidateVisual();
        }
        else if (_dragging && _session.SelectedObject is { } item)
        {
            Vector delta = (current - _pointerStart) / _zoom;
            item.Position = new(_objectStart.X + delta.X, _objectStart.Y + delta.Y);
            InvalidateVisual();
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        bool moved = _dragging;
        bool resized = _resizing;
        _panning = false;
        _dragging = false;
        _resizing = false;
        _resizeHandle = ResizeHandle.None;
        e.Pointer.Capture(null);
        if (moved) _session.CommitPropertyChange("Moved object");
        else if (resized) _session.CommitPropertyChange("Resized object");
    }

    private void OnPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        Point pointer = e.GetPosition(this);
        ScenePoint before = ScreenToWorld(pointer);
        _zoom = Math.Clamp(e.Delta.Y > 0 ? _zoom * 1.15 : _zoom / 1.15, 0.1, 5);
        _cameraOffset = new(pointer.X - before.X * _zoom, pointer.Y - before.Y * _zoom);
        InvalidateVisual();
        e.Handled = true;
    }

    private Point WorldToScreen(ScenePoint point) => new(point.X * _zoom + _cameraOffset.X, point.Y * _zoom + _cameraOffset.Y);

    private ScenePoint ScreenToWorld(Point point) => new((point.X - _cameraOffset.X) / _zoom, (point.Y - _cameraOffset.Y) / _zoom);

    private Rect WorldRectToScreen(SceneObject item)
    {
        Point point = WorldToScreen(item.Position);
        return new Rect(point, new Size(item.Size.Width * _zoom, item.Size.Height * _zoom));
    }

    private static bool Contains(SceneObject item, ScenePoint point) =>
        point.X >= item.Position.X && point.X <= item.Position.X + item.Size.Width &&
        point.Y >= item.Position.Y && point.Y <= item.Position.Y + item.Size.Height;

    private ResizeHandle HitTestHandle(SceneObject item, Point pointer)
    {
        Rect rect = WorldRectToScreen(item);
        (ResizeHandle Handle, Point Point)[] handles =
        [
            (ResizeHandle.TopLeft, rect.TopLeft),
            (ResizeHandle.TopRight, rect.TopRight),
            (ResizeHandle.BottomLeft, rect.BottomLeft),
            (ResizeHandle.BottomRight, rect.BottomRight)
        ];
        foreach ((ResizeHandle handle, Point point) in handles)
        {
            if (new Rect(point.X - 7, point.Y - 7, 14, 14).Contains(pointer)) return handle;
        }
        return ResizeHandle.None;
    }

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
        TopRight,
        BottomLeft,
        BottomRight
    }
}
