namespace TwoDCore.Core.Scene;

public readonly record struct ScenePoint(double X, double Y);

public readonly record struct SceneSize(double Width, double Height)
{
    public SceneSize Clamp(double minimum = 1) => new(
        Math.Max(minimum, Width),
        Math.Max(minimum, Height));
}
