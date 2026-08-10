namespace TwoDCore.Core.Scene;

public abstract class SceneObject
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "Object";
    public abstract SceneObjectKind Kind { get; }
    public ScenePoint Position { get; set; }
    public SceneSize Size { get; set; } = new(60, 60);
    public double Transparency { get; set; }
    public string ColorHex { get; set; } = "#FFFFFF";
    public string TexturePath { get; set; } = string.Empty;
    public List<SceneObject> Children { get; } = [];

    public abstract SceneObject DeepClone();

    protected void CopyBaseTo(SceneObject target)
    {
        target.Id = Id;
        target.Name = Name;
        target.Position = Position;
        target.Size = Size;
        target.Transparency = Transparency;
        target.ColorHex = ColorHex;
        target.TexturePath = TexturePath;
        foreach (SceneObject child in Children)
        {
            target.Children.Add(child.DeepClone());
        }
    }
}

public sealed class ShapeObject : SceneObject
{
    public override SceneObjectKind Kind => SceneObjectKind.Object;

    public override SceneObject DeepClone()
    {
        ShapeObject clone = new();
        CopyBaseTo(clone);
        return clone;
    }
}

public sealed class ImageObject : SceneObject
{
    public override SceneObjectKind Kind => SceneObjectKind.Image;

    public override SceneObject DeepClone()
    {
        ImageObject clone = new();
        CopyBaseTo(clone);
        return clone;
    }
}

public sealed class FolderObject : SceneObject
{
    public override SceneObjectKind Kind => SceneObjectKind.Folder;

    public override SceneObject DeepClone()
    {
        FolderObject clone = new();
        CopyBaseTo(clone);
        return clone;
    }
}
