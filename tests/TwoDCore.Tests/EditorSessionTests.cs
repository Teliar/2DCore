using TwoDCore.Core.Scene;
using TwoDCore.Editor.ViewModels;

namespace TwoDCore.Tests;

public sealed class EditorSessionTests
{
    [Fact]
    public void MultiSelectionDuplicatesOnlyTopLevelObjects()
    {
        EditorSession session = new();
        SceneObject folder = session.Add(SceneObjectKind.Folder);
        SceneObject child = session.Add(SceneObjectKind.Object, folder);
        session.SetSelection([folder, child]);

        session.DuplicateSelected();

        Assert.Equal(2, session.Scene.Objects.Count(item => item.Kind == SceneObjectKind.Folder));
        Assert.Single(session.SelectedObjects);
        Assert.Single(session.SelectedObjects[0].Children);
    }

    [Fact]
    public void ExplorerKeepsExpandedFoldersAcrossRebuild()
    {
        EditorSession session = new();
        SceneObject folder = session.Add(SceneObjectKind.Folder);
        ExplorerNodeViewModel sceneRoot = session.ExplorerRoots[0];
        ExplorerNodeViewModel folderNode = Assert.Single(sceneRoot.Children, node => node.Item == folder);
        folderNode.IsExpanded = true;

        session.RebuildExplorer();

        sceneRoot = session.ExplorerRoots[0];
        folderNode = Assert.Single(sceneRoot.Children, node => node.Item == folder);
        Assert.True(sceneRoot.IsExpanded);
        Assert.True(folderNode.IsExpanded);
    }

    [Fact]
    public void DeleteRemovesAllSelectedObjects()
    {
        EditorSession session = new();
        SceneObject first = session.Add(SceneObjectKind.Object);
        SceneObject second = session.Add(SceneObjectKind.Image);
        session.SetSelection([first, second]);

        session.DeleteSelected();

        Assert.DoesNotContain(first, session.Scene.Objects);
        Assert.DoesNotContain(second, session.Scene.Objects);
        Assert.Empty(session.SelectedObjects);
    }
}
