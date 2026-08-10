using TwoDCore.Core.Audio;

namespace TwoDCore.Core.Scene;

public static class SceneObjectFactory
{
    public static SceneObject Create(SceneObjectKind kind, SceneDocument scene)
    {
        SceneObject item = kind switch
        {
            SceneObjectKind.Image => new ImageObject { Size = new(80, 80) },
            SceneObjectKind.Folder => new FolderObject(),
            SceneObjectKind.SoundService => new SoundServiceObject(),
            SceneObjectKind.Sound => new GlobalSoundObject { Size = new(50, 50) },
            SceneObjectKind.SoundTrigger => new SoundTriggerObject { Size = new(50, 50) },
            SceneObjectKind.SpatialSoundTrigger => new SpatialSoundObject { Size = new(50, 50) },
            _ => new ShapeObject { Size = new(80, 80) }
        };
        item.Name = SceneGraph.GetUniqueName(scene, kind.ToString());
        item.Position = new(-item.Size.Width / 2, -item.Size.Height / 2);
        return item;
    }
}
