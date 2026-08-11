using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using TwoDCore.Core.Audio;
using TwoDCore.Core.Editing;
using TwoDCore.Core.Scene;
using TwoDCore.Persistence;

namespace TwoDCore.Editor.ViewModels;

public sealed partial class EditorSession : ObservableObject
{
    private readonly ProjectFileService _projectFiles = new();
    private readonly SceneHistory _history = new();
    private readonly List<SceneObject> _clipboard = [];

    [ObservableProperty]
    private SceneDocument _scene = CreateNewScene();

    [ObservableProperty]
    private SceneObject? _selectedObject;

    [ObservableProperty]
    private string _projectName = "New Project";

    [ObservableProperty]
    private string? _projectPath;

    [ObservableProperty]
    private bool _isModified;

    [ObservableProperty]
    private string _statusText = "Avalonia editor ready";

    public ObservableCollection<ExplorerNodeViewModel> ExplorerRoots { get; } = [];
    public ObservableCollection<SceneObject> SelectedObjects { get; } = [];

    public event EventHandler? SceneChanged;
    public event EventHandler? SelectionChanged;

    public EditorSession() => RebuildExplorer();

    public string WindowTitle
    {
        get
        {
            string displayName = string.IsNullOrWhiteSpace(ProjectPath) ? ProjectName : Path.GetFileName(ProjectPath);
            return $"2DCore Engine - {displayName}{(IsModified ? " *" : string.Empty)}";
        }
    }

    public void NewProject()
    {
        Scene = CreateNewScene();
        ProjectName = "New Project";
        ProjectPath = null;
        ClearSelection(false);
        IsModified = false;
        _history.Clear();
        NotifyAll("Created a new project");
    }

    public async Task OpenAsync(string path)
    {
        LoadedProject loaded = await _projectFiles.OpenAsync(path);
        Scene = loaded.Scene;
        ProjectName = loaded.ProjectName;
        ProjectPath = loaded.FilePath;
        ClearSelection(false);
        IsModified = false;
        _history.Clear();
        NotifyAll($"Opened {Path.GetFileName(path)}");
    }

    public async Task SaveAsync(string path, int viewportWidth, int viewportHeight)
    {
        string name = Path.GetFileNameWithoutExtension(path);
        await _projectFiles.SaveAsync(path, Scene, name, viewportWidth, viewportHeight);
        ProjectName = name;
        ProjectPath = Path.GetFullPath(path);
        IsModified = false;
        StatusText = $"Saved {Path.GetFileName(path)}";
        OnPropertyChanged(nameof(WindowTitle));
    }

    public SceneObject Add(SceneObjectKind kind, SceneObject? parent = null)
    {
        Capture();
        SceneObject item = SceneObjectFactory.Create(kind, Scene);
        if (kind == SceneObjectKind.Sound)
        {
            Scene.EnsureSoundService().Children.Add(item);
        }
        else if (parent != null && parent is not SoundServiceObject)
        {
            parent.Children.Add(item);
        }
        else
        {
            Scene.Objects.Add(item);
        }
        Select(item);
        NotifyAll($"Added {item.Name}");
        return item;
    }

    public void DeleteSelected()
    {
        SceneObject[] removable = SelectedObjects.Where(item => item is not SoundServiceObject).ToArray();
        if (removable.Length == 0) return;
        Capture();
        foreach (SceneObject item in removable) SceneGraph.Remove(Scene, item);
        ClearSelection(false);
        NotifyAll(removable.Length == 1 ? $"Deleted {removable[0].Name}" : $"Deleted {removable.Length} objects");
    }

    public bool Move(SceneObject item, SceneObject? newParent)
    {
        SceneDocument before = Scene.DeepClone();
        if (!SceneGraph.Move(Scene, item, newParent)) return false;
        _history.Capture(before);
        IsModified = true;
        Select(item);
        NotifyAll(newParent == null ? $"Moved {item.Name} to scene root" : $"Moved {item.Name} into {newParent.Name}");
        return true;
    }

    public void DuplicateSelected()
    {
        SceneObject[] source = GetTopLevelSelection();
        if (source.Length == 0) return;
        Capture();
        List<SceneObject> clones = [];
        foreach (SceneObject item in source)
        {
            SceneObject clone = item.DeepClone();
            SceneGraph.AssignNewIds(clone);
            clone.Name = SceneGraph.GetUniqueName(Scene, item.Name);
            clone.Position = new(clone.Position.X + 20, clone.Position.Y + 20);
            SceneObject? parent = SceneGraph.FindParent(Scene.Objects, item);
            if (parent == null) Scene.Objects.Add(clone);
            else parent.Children.Add(clone);
            clones.Add(clone);
        }
        SetSelection(clones);
        NotifyAll(clones.Count == 1 ? $"Duplicated {source[0].Name}" : $"Duplicated {clones.Count} objects");
    }

    public void CopySelected()
    {
        _clipboard.Clear();
        _clipboard.AddRange(GetTopLevelSelection().Select(item => item.DeepClone()));
        StatusText = _clipboard.Count == 0 ? "Nothing to copy" : _clipboard.Count == 1 ? $"Copied {_clipboard[0].Name}" : $"Copied {_clipboard.Count} objects";
    }

    public void Paste()
    {
        if (_clipboard.Count == 0) return;
        Capture();
        List<SceneObject> pasted = [];
        foreach (SceneObject source in _clipboard)
        {
            SceneObject clone = source.DeepClone();
            SceneGraph.AssignNewIds(clone);
            clone.Name = SceneGraph.GetUniqueName(Scene, clone.Name);
            clone.Position = new(clone.Position.X + 20, clone.Position.Y + 20);
            if (clone is GlobalSoundObject) Scene.EnsureSoundService().Children.Add(clone);
            else Scene.Objects.Add(clone);
            pasted.Add(clone);
        }
        SetSelection(pasted);
        NotifyAll(pasted.Count == 1 ? $"Pasted {pasted[0].Name}" : $"Pasted {pasted.Count} objects");
    }

    public void Undo()
    {
        SceneDocument? restored = _history.Undo(Scene);
        if (restored == null) return;
        Scene = restored;
        ClearSelection(false);
        NotifyAll("Undo");
    }

    public void Redo()
    {
        SceneDocument? restored = _history.Redo(Scene);
        if (restored == null) return;
        Scene = restored;
        ClearSelection(false);
        NotifyAll("Redo");
    }

    public void Capture()
    {
        _history.Capture(Scene);
        IsModified = true;
        OnPropertyChanged(nameof(WindowTitle));
    }

    public void CommitPropertyChange(string message = "Property changed")
    {
        IsModified = true;
        StatusText = message;
        RebuildExplorer();
        SceneChanged?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(WindowTitle));
    }

    public bool IsSelected(SceneObject item) => SelectedObjects.Contains(item);

    public void Select(SceneObject? item, bool additive = false, bool toggle = false)
    {
        if (!additive) SelectedObjects.Clear();
        if (item != null)
        {
            if (toggle && SelectedObjects.Contains(item)) SelectedObjects.Remove(item);
            else if (!SelectedObjects.Contains(item)) SelectedObjects.Add(item);
        }
        SelectedObject = SelectedObjects.LastOrDefault();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        SceneChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetSelection(IEnumerable<SceneObject> items)
    {
        SelectedObjects.Clear();
        foreach (SceneObject item in items.Distinct()) SelectedObjects.Add(item);
        SelectedObject = SelectedObjects.LastOrDefault();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        SceneChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearSelection(bool notify = true)
    {
        SelectedObjects.Clear();
        SelectedObject = null;
        if (!notify) return;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        SceneChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RebuildExplorer()
    {
        HashSet<Guid> expandedIds = ExplorerRoots
            .SelectMany(TraverseExplorer)
            .Where(node => node.IsExpanded && node.Item != null)
            .Select(node => node.Item!.Id)
            .ToHashSet();
        ExplorerRoots.Clear();
        ExplorerRoots.Add(new ExplorerNodeViewModel(
            "Scene",
            "avares://2DCore/EditorAssets/Icons/camera.png",
            null,
            Scene.Objects.Where(item => item is not SoundServiceObject).Select(item => CreateExplorerNode(item, expandedIds)),
            isExpanded: true));
        foreach (SoundServiceObject service in Scene.Objects.OfType<SoundServiceObject>())
        {
            ExplorerRoots.Add(CreateExplorerNode(service, expandedIds));
        }
    }

    private void NotifyAll(string status)
    {
        StatusText = status;
        RebuildExplorer();
        SceneChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(WindowTitle));
    }

    private SceneObject[] GetTopLevelSelection() => SelectedObjects
        .Where(item => item is not SoundServiceObject)
        .Where(item => !SelectedObjects.Any(other => !ReferenceEquals(item, other) && SceneGraph.IsDescendant(other, item)))
        .ToArray();

    private static IEnumerable<ExplorerNodeViewModel> TraverseExplorer(ExplorerNodeViewModel node)
    {
        yield return node;
        foreach (ExplorerNodeViewModel child in node.Children.SelectMany(TraverseExplorer)) yield return child;
    }

    private static ExplorerNodeViewModel CreateExplorerNode(SceneObject item, HashSet<Guid>? expandedIds = null) => new(
        item.Name,
        GetIcon(item),
        item,
        item.Children.Select(child => CreateExplorerNode(child, expandedIds)),
        expandedIds?.Contains(item.Id) == true);

    private static string GetIcon(SceneObject item) => item.Kind switch
    {
        SceneObjectKind.Folder => item.Children.Count > 0
            ? "avares://2DCore/EditorAssets/Icons/folder_page.png"
            : "avares://2DCore/EditorAssets/Icons/folder.png",
        SceneObjectKind.Image => "avares://2DCore/EditorAssets/Icons/images.png",
        SceneObjectKind.SoundService => "avares://2DCore/EditorAssets/Icons/sound_add.png",
        SceneObjectKind.Sound => "avares://2DCore/EditorAssets/Icons/sound.png",
        SceneObjectKind.SoundTrigger or SceneObjectKind.SpatialSoundTrigger => "avares://2DCore/EditorAssets/Icons/SoundTrigger.png",
        _ => "avares://2DCore/EditorAssets/Icons/IconCore.png"
    };

    private static SceneDocument CreateNewScene()
    {
        SceneDocument scene = new();
        scene.EnsureSoundService();
        return scene;
    }
}
