using System.Text.Json;
using System.Text.Json.Serialization;
using TwoDCore.Core.Scene;
using TwoDCore.Persistence.Dtos;

namespace TwoDCore.Persistence;

public sealed record LoadedProject(SceneDocument Scene, string ProjectName, string FilePath);

public sealed class ProjectFileService
{
    private readonly SceneDtoMapper _mapper = new();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<LoadedProject> OpenAsync(string filePath, CancellationToken cancellationToken = default)
    {
        string fullPath = Path.GetFullPath(filePath);
        string projectDirectory = Path.GetDirectoryName(fullPath) ?? Environment.CurrentDirectory;
        string scenePath = fullPath;
        string projectName = Path.GetFileNameWithoutExtension(fullPath);

        if (Path.GetExtension(fullPath).Equals(".2dproj", StringComparison.OrdinalIgnoreCase))
        {
            ProjectDataDto project = await DeserializeAsync<ProjectDataDto>(fullPath, cancellationToken)
                ?? throw new InvalidDataException("Project file is empty or malformed.");
            if (project.FormatVersion > 1) throw new NotSupportedException($"Unsupported project format v{project.FormatVersion}.");
            projectName = project.ProjectName;
            scenePath = Path.IsPathRooted(project.StartScene)
                ? project.StartScene
                : Path.GetFullPath(Path.Combine(projectDirectory, project.StartScene));
        }

        SceneDataDto sceneDto = await DeserializeAsync<SceneDataDto>(scenePath, cancellationToken)
            ?? throw new InvalidDataException("Scene file is empty or malformed.");
        if (sceneDto.FormatVersion > 1) throw new NotSupportedException($"Unsupported scene format v{sceneDto.FormatVersion}.");
        return new LoadedProject(_mapper.FromDto(sceneDto, projectDirectory), projectName, fullPath);
    }

    public async Task SaveAsync(
        string filePath,
        SceneDocument scene,
        string projectName,
        int viewportWidth = 1200,
        int viewportHeight = 780,
        CancellationToken cancellationToken = default)
    {
        string fullPath = Path.GetFullPath(filePath);
        string projectDirectory = Path.GetDirectoryName(fullPath) ?? Environment.CurrentDirectory;
        bool isProject = Path.GetExtension(fullPath).Equals(".2dproj", StringComparison.OrdinalIgnoreCase);

        if (!isProject)
        {
            await WriteAtomicAsync(fullPath, JsonSerializer.Serialize(_mapper.ToDto(scene, projectDirectory), _jsonOptions), cancellationToken);
            return;
        }

        string scenesDirectory = Path.Combine(projectDirectory, "scenes");
        Directory.CreateDirectory(scenesDirectory);
        string scenePath = Path.Combine(scenesDirectory, "main.2dscene");
        string relativeScenePath = "scenes/main.2dscene";

        await WriteAtomicAsync(scenePath, JsonSerializer.Serialize(_mapper.ToDto(scene, projectDirectory), _jsonOptions), cancellationToken);

        ProjectDataDto project = new()
        {
            ProjectName = projectName,
            StartScene = relativeScenePath,
            Scenes = [relativeScenePath],
            Settings = new ProjectSettingsDto
            {
                ViewportWidth = viewportWidth,
                ViewportHeight = viewportHeight,
                BackgroundColorHex = "#121317"
            }
        };
        await WriteAtomicAsync(fullPath, JsonSerializer.Serialize(project, _jsonOptions), cancellationToken);
    }

    private async Task<T?> DeserializeAsync<T>(string path, CancellationToken cancellationToken)
    {
        await using FileStream stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken);
    }

    private static async Task WriteAtomicAsync(string path, string content, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);

        string temporaryPath = path + ".tmp";
        await File.WriteAllTextAsync(temporaryPath, content, cancellationToken);
        File.Move(temporaryPath, path, true);
    }
}
