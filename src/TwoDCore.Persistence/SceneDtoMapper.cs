using TwoDCore.Core.Audio;
using TwoDCore.Core.Scene;
using TwoDCore.Persistence.Dtos;

namespace TwoDCore.Persistence;

public sealed class SceneDtoMapper
{
    public SceneDataDto ToDto(SceneDocument scene, string projectDirectory)
    {
        return new SceneDataDto
        {
            SceneName = scene.Name,
            Objects = scene.Objects.Select(item => ToDto(item, null, projectDirectory)).ToList()
        };
    }

    public SceneDocument FromDto(SceneDataDto dto, string projectDirectory)
    {
        SceneDocument scene = new() { Name = dto.SceneName };
        HashSet<Guid> loadedIds = [];
        foreach (GameObjectDto item in dto.Objects)
        {
            scene.Objects.Add(FromDto(item, projectDirectory, loadedIds));
        }
        scene.EnsureSoundService();
        return scene;
    }

    private GameObjectDto ToDto(SceneObject item, Guid? parentId, string projectDirectory)
    {
        GameObjectDto dto = new()
        {
            Id = item.Id,
            ParentId = parentId,
            Name = item.Name,
            ObjectType = ToLegacyObjectType(item.Kind)
        };

        bool hasTransform = item.Kind is not SceneObjectKind.Folder and not SceneObjectKind.Sound and not SceneObjectKind.SoundService;
        if (hasTransform)
        {
            dto.Components.Add(new TransformComponentDto
            {
                X = (int)Math.Round(item.Position.X),
                Y = (int)Math.Round(item.Position.Y),
                Width = (int)Math.Round(item.Size.Width),
                Height = (int)Math.Round(item.Size.Height),
                Transparency = (float)item.Transparency
            });
        }

        if (item.Kind is SceneObjectKind.Object or SceneObjectKind.Image)
        {
            dto.Components.Add(new RenderComponentDto
            {
                ColorHex = item.ColorHex,
                TexturePath = MakeRelative(item.TexturePath, projectDirectory)
            });
        }

        if (item is SoundObjectBase sound)
        {
            SpatialSoundObject? spatial = sound as SpatialSoundObject;
            dto.Components.Add(new SoundComponentDto
            {
                AudioFilePath = MakeRelative(sound.AudioFilePath, projectDirectory),
                Volume = sound.Volume,
                Radius = spatial is null ? null : (float)spatial.Radius,
                FullVolumeRadius = spatial is null ? null : (float)spatial.FullVolumeRadius,
                Rolloff = spatial?.Rolloff
            });
        }

        dto.Children.AddRange(item.Children.Select(child => ToDto(child, item.Id, projectDirectory)));
        return dto;
    }

    private SceneObject FromDto(GameObjectDto dto, string projectDirectory, HashSet<Guid> loadedIds)
    {
        SceneObject item = CreateFromLegacyType(dto.ObjectType);
        item.Id = dto.Id == Guid.Empty || !loadedIds.Add(dto.Id) ? Guid.NewGuid() : dto.Id;
        loadedIds.Add(item.Id);
        item.Name = dto.Name ?? "Object";

        foreach (ComponentDto component in dto.Components)
        {
            switch (component)
            {
                case TransformComponentDto transform:
                    item.Position = new(transform.X, transform.Y);
                    item.Size = new SceneSize(transform.Width, transform.Height).Clamp(1);
                    item.Transparency = Math.Clamp(transform.Transparency, 0, 1);
                    break;
                case RenderComponentDto render:
                    item.ColorHex = string.IsNullOrWhiteSpace(render.ColorHex) ? "#FFFFFF" : render.ColorHex;
                    item.TexturePath = ResolvePath(render.TexturePath, projectDirectory);
                    break;
                case SoundComponentDto soundDto when item is SoundObjectBase sound:
                    sound.AudioFilePath = ResolvePath(soundDto.AudioFilePath, projectDirectory);
                    sound.Volume = soundDto.Volume;
                    if (sound is SpatialSoundObject spatial)
                    {
                        spatial.Radius = soundDto.Radius ?? 250;
                        spatial.FullVolumeRadius = soundDto.FullVolumeRadius ?? 50;
                        spatial.Rolloff = soundDto.Rolloff ?? SoundRolloff.Smooth;
                    }
                    break;
            }
        }

        foreach (GameObjectDto child in dto.Children)
        {
            item.Children.Add(FromDto(child, projectDirectory, loadedIds));
        }
        return item;
    }

    private static SceneObject CreateFromLegacyType(string objectType) => objectType switch
    {
        "Image" => new ImageObject(),
        "Folder" => new FolderObject(),
        "SoundService" => new SoundServiceObject(),
        "Sound" => new GlobalSoundObject(),
        "SoundTrigger" => new SoundTriggerObject(),
        "SpatialSoundTrigger" => new SpatialSoundObject(),
        _ => new ShapeObject()
    };

    private static string ToLegacyObjectType(SceneObjectKind kind) => kind switch
    {
        SceneObjectKind.Image => "Image",
        SceneObjectKind.Folder => "Folder",
        SceneObjectKind.SoundService => "SoundService",
        SceneObjectKind.Sound => "Sound",
        SceneObjectKind.SoundTrigger => "SoundTrigger",
        SceneObjectKind.SpatialSoundTrigger => "SpatialSoundTrigger",
        _ => "Object"
    };

    private static string ResolvePath(string path, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(path) || Path.IsPathRooted(path)) return path;
        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    private static string MakeRelative(string path, string baseDirectory)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(baseDirectory)) return path;
        try
        {
            return Path.GetRelativePath(baseDirectory, path).Replace('\\', '/');
        }
        catch
        {
            return path;
        }
    }
}
