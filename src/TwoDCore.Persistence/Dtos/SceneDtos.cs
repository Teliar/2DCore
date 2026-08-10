using System.Text.Json.Serialization;
using TwoDCore.Core.Audio;

namespace TwoDCore.Persistence.Dtos;

public sealed class SceneDataDto
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; } = 1;

    [JsonPropertyName("sceneName")]
    public string SceneName { get; set; } = "MainScene";

    [JsonPropertyName("objects")]
    public List<GameObjectDto> Objects { get; set; } = [];
}

public sealed class GameObjectDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("parentId")]
    public Guid? ParentId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Object";

    [JsonPropertyName("objectType")]
    public string ObjectType { get; set; } = "Object";

    [JsonPropertyName("components")]
    public List<ComponentDto> Components { get; set; } = [];

    [JsonPropertyName("children")]
    public List<GameObjectDto> Children { get; set; } = [];
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(TransformComponentDto), "Transform")]
[JsonDerivedType(typeof(RenderComponentDto), "Render")]
[JsonDerivedType(typeof(SoundComponentDto), "Sound")]
public abstract class ComponentDto;

public sealed class TransformComponentDto : ComponentDto
{
    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; } = 60;

    [JsonPropertyName("height")]
    public int Height { get; set; } = 60;

    [JsonPropertyName("transparency")]
    public float Transparency { get; set; }
}

public sealed class RenderComponentDto : ComponentDto
{
    [JsonPropertyName("colorHex")]
    public string ColorHex { get; set; } = "#FFFFFF";

    [JsonPropertyName("texturePath")]
    public string TexturePath { get; set; } = string.Empty;
}

public sealed class SoundComponentDto : ComponentDto
{
    [JsonPropertyName("audioFilePath")]
    public string AudioFilePath { get; set; } = string.Empty;

    [JsonPropertyName("volume")]
    public double Volume { get; set; } = 1;

    [JsonPropertyName("radius")]
    public float? Radius { get; set; }

    [JsonPropertyName("fullVolumeRadius")]
    public float? FullVolumeRadius { get; set; }

    [JsonPropertyName("rolloff")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public SoundRolloff? Rolloff { get; set; }
}
