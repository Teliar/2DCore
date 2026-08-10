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
    private SceneObject? _clipboard;

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

    public event EventHandler? SceneChanged;
    public event EventHandler? SelectionChanged;

    public EditorSession() => RebuildExplorer();

    public string WindowTitle => $"2DCore — {ProjectName}{(IsModified ? " *" : string.Empty)}";

    public void NewProject()
    {
        Scene = CreateNewScene();
        ProjectName = "New Project";
        ProjectPath = null;
        SelectedObject = null;
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
        SelectedObject = null;
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
        else if (parent is FolderObject)
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
        if (SelectedObject == null || SelectedObject is SoundServiceObject) return;
        Capture();
        string name = SelectedObject.Name;
        SceneGraph.Remove(Scene, SelectedObject);
        SelectedObject = null;
        NotifyAll($"Deleted {name}");
    }

    public bool Move(SceneObject item, SceneObject? newParent)
    {
        if (newParent is not (null or FolderObject or SoundServiceObject)) return false;
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
        if (SelectedObject == null || SelectedObject is SoundServiceObject) return;
        Capture();
        SceneObject clone = SelectedObject.DeepClone();
        SceneGraph.AssignNewIds(clone);
        clone.Name = SceneGraph.GetUniqueName(Scene, SelectedObject.Name);
        clone.Position = new(clone.Position.X + 20, clone.Position.Y + 20);
        SceneObject? parent = SceneGraph.FindParent(Scene.Objects, SelectedObject);
        if (parent == null) Scene.Objects.Add(clone);
        else parent.Children.Add(clone);
        Select(clone);
        NotifyAll($"Duplicated {SelectedObject.Name}");
    }

    public void CopySelected()
    {
        _clipboard = SelectedObject is SoundServiceObject or null ? null : SelectedObject.DeepClone();
        StatusText = _clipboard == null ? "Nothing to copy" : $"Copied {_clipboard.Name}";
    }

    public void Paste()
    {
        if (_clipboard == null) return;
        Capture();
        SceneObject clone = _clipboard.DeepClone();
        SceneGraph.AssignNewIds(clone);
        clone.Name = SceneGraph.GetUniqueName(Scene, clone.Name);
        clone.Position = new(clone.Position.X + 20, clone.Position.Y + 20);
        if (clone is GlobalSoundObject) Scene.EnsureSoundService().Children.Add(clone);
        else Scene.Objects.Add(clone);
        Select(clone);
        NotifyAll($"Pasted {clone.Name}");
    }

    public void Undo()
    {
        SceneDocument? restored = _history.Undo(Scene);
        if (restored == null) return;
        Scene = restored;
        SelectedObject = null;
        NotifyAll("Undo");
    }

    public void Redo()
    {
        SceneDocument? restored = _history.Redo(Scene);
        if (restored == null) return;
        Scene = restored;
        SelectedObject = null;
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

    public void Select(SceneObject? item)
    {
        SelectedObject = item;
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        SceneChanged?.Invoke(this, EventArgs.Empty);
    }

    public void RebuildExplorer()
    {
        ExplorerRoots.Clear();
        ExplorerRoots.Add(new ExplorerNodeViewModel(
            "Scene",
            "◉",
            null,
            Scene.Objects.Where(item => item is not SoundServiceObject).Select(CreateExplorerNode)));
        foreach (SoundServiceObject service in Scene.Objects.OfType<SoundServiceObject>())
        {
            ExplorerRoots.Add(CreateExplorerNode(service));
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

    private static ExplorerNodeViewModel CreateExplorerNode(SceneObject item) => new(
        item.Name,
        GetIcon(item.Kind),
        item,
        item.Children.Select(CreateExplorerNode));

    private static string GetIcon(SceneObjectKind kind) => kind switch
    {
        SceneObjectKind.Folder => "▰",
        SceneObjectKind.Image => "▧",
        SceneObjectKind.SoundService => "♫",
        SceneObjectKind.Sound or SceneObjectKind.SoundTrigger or SceneObjectKind.SpatialSoundTrigger => "◖",
        _ => "◆"
    };

    private static SceneDocument CreateNewScene()
    {
        SceneDocument scene = new();
        scene.EnsureSoundService();
        return scene;
    }
}
