using TwoDCore.Core.Audio;

namespace TwoDCore.Core.Scene;

public sealed class SceneDocument
{
    public string Name { get; set; } = "MainScene";
    public List<SceneObject> Objects { get; } = [];

    public SoundServiceObject EnsureSoundService()
    {
        SoundServiceObject? existing = Objects.OfType<SoundServiceObject>().FirstOrDefault();
        if (existing != null) return existing;

        SoundServiceObject service = new();
        Objects.Add(service);
        return service;
    }

    public SceneDocument DeepClone()
    {
        SceneDocument clone = new() { Name = Name };
        clone.Objects.AddRange(Objects.Select(item => item.DeepClone()));
        return clone;
    }
}
