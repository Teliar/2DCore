using TwoDCore.Core.Scene;

namespace TwoDCore.Core.Audio;

public enum SoundRolloff
{
    Linear,
    Smooth
}

public abstract class SoundObjectBase : SceneObject
{
    public string AudioFilePath { get; set; } = string.Empty;

    private double _volume = 1.0;
    public double Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0.0, 1.0);
    }

    protected void CopySoundTo(SoundObjectBase target)
    {
        CopyBaseTo(target);
        target.AudioFilePath = AudioFilePath;
        target.Volume = Volume;
    }
}

public sealed class GlobalSoundObject : SoundObjectBase
{
    public override SceneObjectKind Kind => SceneObjectKind.Sound;

    public override SceneObject DeepClone()
    {
        GlobalSoundObject clone = new();
        CopySoundTo(clone);
        return clone;
    }
}

public sealed class SoundTriggerObject : SoundObjectBase
{
    public override SceneObjectKind Kind => SceneObjectKind.SoundTrigger;

    public override SceneObject DeepClone()
    {
        SoundTriggerObject clone = new();
        CopySoundTo(clone);
        return clone;
    }
}

public sealed class SpatialSoundObject : SoundObjectBase
{
    private double _radius = 250;
    private double _fullVolumeRadius = 50;

    public override SceneObjectKind Kind => SceneObjectKind.SpatialSoundTrigger;

    public double Radius
    {
        get => _radius;
        set
        {
            _radius = Math.Max(10, value);
            _fullVolumeRadius = Math.Min(_fullVolumeRadius, _radius);
        }
    }

    public double FullVolumeRadius
    {
        get => _fullVolumeRadius;
        set => _fullVolumeRadius = Math.Clamp(value, 0, Radius);
    }

    public SoundRolloff Rolloff { get; set; } = SoundRolloff.Smooth;

    public double CalculateVolumeAtDistance(double distance)
    {
        if (distance <= FullVolumeRadius) return Volume;
        if (distance >= Radius) return 0;

        double t = (distance - FullVolumeRadius) / Math.Max(0.001, Radius - FullVolumeRadius);
        double attenuation = Rolloff == SoundRolloff.Smooth
            ? 1 - (t * t * (3 - 2 * t))
            : 1 - t;
        return Volume * Math.Clamp(attenuation, 0, 1);
    }

    public override SceneObject DeepClone()
    {
        SpatialSoundObject clone = new();
        CopySoundTo(clone);
        clone.Radius = Radius;
        clone.FullVolumeRadius = FullVolumeRadius;
        clone.Rolloff = Rolloff;
        return clone;
    }
}

public sealed class SoundServiceObject : SceneObject
{
    public SoundServiceObject()
    {
        Name = "SoundService";
    }

    public override SceneObjectKind Kind => SceneObjectKind.SoundService;

    public override SceneObject DeepClone()
    {
        SoundServiceObject clone = new();
        CopyBaseTo(clone);
        return clone;
    }
}
