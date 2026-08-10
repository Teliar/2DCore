using TwoDCore.Core.Audio;
using TwoDCore.Core.Scene;

namespace TwoDCore.Tests;

public sealed class SceneGraphTests
{
    [Fact]
    public void FolderCanBeMovedIntoAnotherFolderWithoutAllowingCycles()
    {
        SceneDocument scene = new();
        FolderObject parent = new() { Name = "Parent" };
        FolderObject child = new() { Name = "Child" };
        scene.Objects.Add(parent);
        scene.Objects.Add(child);

        Assert.True(SceneGraph.Move(scene, child, parent));
        Assert.Contains(child, parent.Children);
        Assert.False(SceneGraph.Move(scene, parent, child));
        Assert.Contains(parent, scene.Objects);
    }

    [Fact]
    public void SpatialSoundAttenuatesFromInnerToOuterRadius()
    {
        SpatialSoundObject sound = new()
        {
            Volume = 0.8,
            FullVolumeRadius = 50,
            Radius = 250,
            Rolloff = SoundRolloff.Smooth
        };

        Assert.Equal(0.8, sound.CalculateVolumeAtDistance(0), 3);
        Assert.Equal(0.8, sound.CalculateVolumeAtDistance(50), 3);
        Assert.Equal(0.4, sound.CalculateVolumeAtDistance(150), 3);
        Assert.Equal(0.0, sound.CalculateVolumeAtDistance(250), 3);
    }

    [Fact]
    public void ClonePreservesIdsUntilExplicitlyDuplicated()
    {
        FolderObject folder = new() { Name = "Folder" };
        folder.Children.Add(new ShapeObject { Name = "Object" });

        SceneObject clone = folder.DeepClone();
        Assert.Equal(folder.Id, clone.Id);
        Assert.Equal(folder.Children[0].Id, clone.Children[0].Id);

        SceneGraph.AssignNewIds(clone);
        Assert.NotEqual(folder.Id, clone.Id);
        Assert.NotEqual(folder.Children[0].Id, clone.Children[0].Id);
    }
}
