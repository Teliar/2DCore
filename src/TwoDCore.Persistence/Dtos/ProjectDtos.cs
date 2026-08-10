using System.Text.Json.Serialization;

namespace TwoDCore.Persistence.Dtos;

public sealed class ProjectDataDto
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; } = 1;

    [JsonPropertyName("projectName")]
    public string ProjectName { get; set; } = "New Project";

    [JsonPropertyName("startScene")]
    public string StartScene { get; set; } = "scenes/main.2dscene";

    [JsonPropertyName("scenes")]
    public List<string> Scenes { get; set; } = ["scenes/main.2dscene"];

    [JsonPropertyName("settings")]
    public ProjectSettingsDto Settings { get; set; } = new();
}

public sealed class ProjectSettingsDto
{
    [JsonPropertyName("viewportWidth")]
    public int ViewportWidth { get; set; } = 1200;

    [JsonPropertyName("viewportHeight")]
    public int ViewportHeight { get; set; } = 780;

    [JsonPropertyName("backgroundColorHex")]
    public string BackgroundColorHex { get; set; } = "#121317";
}
