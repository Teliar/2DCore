using TwoDCore.Core.Audio;
using TwoDCore.Core.Scene;
using TwoDCore.Persistence;

namespace TwoDCore.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public async Task SavesAndLoadsProjectWithSpatialSoundAndHierarchy()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            SceneDocument scene = new();
            FolderObject folder = new() { Name = "Audio" };
            folder.Children.Add(new SpatialSoundObject
            {
                Name = "Wind",
                Position = new(120, 80),
                Radius = 420,
                FullVolumeRadius = 60,
                Volume = 0.7,
                Rolloff = SoundRolloff.Linear
            });
            scene.Objects.Add(folder);
            scene.EnsureSoundService();
            string projectPath = Path.Combine(directory, "roundtrip.2dproj");
            ProjectFileService service = new();

            await service.SaveAsync(projectPath, scene, "Roundtrip");
            LoadedProject loaded = await service.OpenAsync(projectPath);

            SpatialSoundObject restored = Assert.IsType<SpatialSoundObject>(
                Assert.Single(Assert.IsType<FolderObject>(loaded.Scene.Objects[0]).Children));
            Assert.Equal(420, restored.Radius);
            Assert.Equal(60, restored.FullVolumeRadius);
            Assert.Equal(0.7, restored.Volume, 3);
            Assert.Equal(SoundRolloff.Linear, restored.Rolloff);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task OpensLegacyVersionOneScene()
    {
        string directory = CreateTemporaryDirectory();
        try
        {
            string scenePath = Path.Combine(directory, "legacy.2dscene");
            await File.WriteAllTextAsync(scenePath, LegacySceneJson);

            LoadedProject loaded = await new ProjectFileService().OpenAsync(scenePath);

            ShapeObject item = Assert.IsType<ShapeObject>(loaded.Scene.Objects[0]);
            Assert.Equal("LegacyObject", item.Name);
            Assert.Equal(new ScenePoint(15, 25), item.Position);
            Assert.Equal(new SceneSize(80, 90), item.Size);
            Assert.Equal("#FF0000", item.ColorHex);
            Assert.Contains(loaded.Scene.Objects, value => value is SoundServiceObject);
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), "2dcore-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private const string LegacySceneJson = """
        {
          "formatVersion": 1,
          "sceneName": "Legacy",
          "objects": [
            {
              "id": "b149fdd4-a5c2-4f92-946c-758f70e4f423",
              "parentId": null,
              "name": "LegacyObject",
              "objectType": "Object",
              "components": [
                { "type": "Transform", "x": 15, "y": 25, "width": 80, "height": 90, "transparency": 0.2 },
                { "type": "Render", "colorHex": "#FF0000", "texturePath": "" }
              ],
              "children": []
            }
          ]
        }
        """;
}
