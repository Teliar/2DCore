using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using TwoDCore.Core.Scene;
using TwoDCore.Editor.Controls;
using TwoDCore.Editor.ViewModels;

namespace TwoDCore.Editor.Views;

public sealed partial class MainWindow : Window
{
    private readonly EditorSession _session = new();
    private readonly SceneViewport _viewport;
    private ExplorerNodeViewModel? _draggedExplorerNode;
    private Point _explorerPressPoint;
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _session;
        _viewport = new SceneViewport(_session);
        ViewportHost.Content = _viewport;
        InspectorHost.Content = new InspectorPanel(_session);
        Closing += MainWindow_Closing;
    }

    private void ExplorerTree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ExplorerTree.SelectedItem is ExplorerNodeViewModel node) _session.Select(node.Item);
    }

    private void ExplorerTree_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(ExplorerTree).Properties.IsLeftButtonPressed) return;
        _draggedExplorerNode = (e.Source as StyledElement)?.DataContext as ExplorerNodeViewModel;
        _explorerPressPoint = e.GetPosition(ExplorerTree);
    }

    private void ExplorerTree_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        ExplorerNodeViewModel? dragged = _draggedExplorerNode;
        _draggedExplorerNode = null;
        if (dragged?.Item == null) return;
        Point releasePoint = e.GetPosition(ExplorerTree);
        double dragDistance = Math.Sqrt(
            Math.Pow(releasePoint.X - _explorerPressPoint.X, 2) +
            Math.Pow(releasePoint.Y - _explorerPressPoint.Y, 2));
        if (dragDistance < 6) return;

        ExplorerNodeViewModel? targetNode = (e.Source as StyledElement)?.DataContext as ExplorerNodeViewModel;
        if (ReferenceEquals(dragged, targetNode)) return;
        SceneObject? target = targetNode?.Item;
        if (target is not (null or FolderObject or TwoDCore.Core.Audio.SoundServiceObject)) return;
        _session.Move(dragged.Item, target);
    }

    private void MainWindow_KeyDown(object? sender, KeyEventArgs e)
    {
        bool control = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        bool shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        if (control && e.Key == Key.N) NewProject_Click(sender, e);
        else if (control && e.Key == Key.O) OpenProject_Click(sender, e);
        else if (control && e.Key == Key.S && shift) SaveProjectAs_Click(sender, e);
        else if (control && e.Key == Key.S) SaveProject_Click(sender, e);
        else if (control && e.Key == Key.Z && shift) _session.Redo();
        else if (control && e.Key == Key.Z) _session.Undo();
        else if (control && e.Key == Key.Y) _session.Redo();
        else if (control && e.Key == Key.C) _session.CopySelected();
        else if (control && e.Key == Key.V) _session.Paste();
        else if (control && e.Key == Key.D) _session.DuplicateSelected();
        else if (e.Key == Key.Delete) _session.DeleteSelected();
        else return;
        e.Handled = true;
    }

    private async void NewProject_Click(object? sender, RoutedEventArgs e)
    {
        if (await EnsureCanDiscardChangesAsync()) _session.NewProject();
    }

    private async void OpenProject_Click(object? sender, RoutedEventArgs e)
    {
        if (!await EnsureCanDiscardChangesAsync()) return;
        if (!StorageProvider.CanOpen) return;
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open 2DCore project or scene",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("2DCore projects") { Patterns = ["*.2dproj", "*.2dscene"] },
                FilePickerFileTypes.All
            ]
        });
        string? path = files.FirstOrDefault()?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) return;
        await RunGuardedAsync(() => _session.OpenAsync(path));
    }

    private async void SaveProject_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_session.ProjectPath))
        {
            await SaveAsAsync();
            return;
        }
        await RunGuardedAsync(() => _session.SaveAsync(
            _session.ProjectPath,
            (int)Math.Round(_viewport.Bounds.Width),
            (int)Math.Round(_viewport.Bounds.Height)));
    }

    private async void SaveProjectAs_Click(object? sender, RoutedEventArgs e) => await SaveAsAsync();

    private async Task<bool> SaveAsAsync()
    {
        if (!StorageProvider.CanSave) return false;
        IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save 2DCore project",
            SuggestedFileName = _session.ProjectName,
            DefaultExtension = "2dproj",
            FileTypeChoices = [new FilePickerFileType("2DCore project") { Patterns = ["*.2dproj"] }]
        });
        string? path = file?.TryGetLocalPath();
        if (string.IsNullOrWhiteSpace(path)) return false;
        return await RunGuardedAsync(() => _session.SaveAsync(
            path,
            (int)Math.Round(_viewport.Bounds.Width),
            (int)Math.Round(_viewport.Bounds.Height)));
    }

    private async Task<bool> RunGuardedAsync(Func<Task> action)
    {
        try
        {
            await action();
            return true;
        }
        catch (Exception exception)
        {
            _session.StatusText = exception.Message;
            await MessageBoxAsync("2DCore", exception.Message);
            return false;
        }
    }

    private async Task<bool> EnsureCanDiscardChangesAsync()
    {
        if (!_session.IsModified) return true;
        UnsavedChoice choice = await ShowUnsavedChangesAsync();
        if (choice == UnsavedChoice.Cancel) return false;
        if (choice == UnsavedChoice.Discard) return true;

        if (string.IsNullOrWhiteSpace(_session.ProjectPath)) return await SaveAsAsync();
        return await RunGuardedAsync(() => _session.SaveAsync(
            _session.ProjectPath,
            (int)Math.Round(_viewport.Bounds.Width),
            (int)Math.Round(_viewport.Bounds.Height)));
    }

    private async Task<UnsavedChoice> ShowUnsavedChangesAsync()
    {
        TaskCompletionSource<UnsavedChoice> completion = new();
        Button save = new() { Content = "Save" };
        Button discard = new() { Content = "Discard" };
        Button cancel = new() { Content = "Cancel" };
        StackPanel buttons = new()
        {
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8,
            Children = { save, discard, cancel }
        };
        Window dialog = new()
        {
            Title = "Unsaved changes",
            Width = 460,
            Height = 180,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Thickness(18),
                Spacing = 22,
                Children =
                {
                    new TextBlock { Text = "The project has unsaved changes. Save them first?", TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    buttons
                }
            }
        };
        void Complete(UnsavedChoice value)
        {
            completion.TrySetResult(value);
            dialog.Close();
        }
        save.Click += (_, _) => Complete(UnsavedChoice.Save);
        discard.Click += (_, _) => Complete(UnsavedChoice.Discard);
        cancel.Click += (_, _) => Complete(UnsavedChoice.Cancel);
        dialog.Closed += (_, _) => completion.TrySetResult(UnsavedChoice.Cancel);
        _ = dialog.ShowDialog(this);
        return await completion.Task;
    }

    private async void MainWindow_Closing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || !_session.IsModified) return;
        e.Cancel = true;
        if (!await EnsureCanDiscardChangesAsync()) return;
        _allowClose = true;
        Close();
    }

    private async Task MessageBoxAsync(string title, string message)
    {
        Window dialog = new()
        {
            Title = title,
            Width = 480,
            Height = 180,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(18),
                Spacing = 18,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new Button { Content = "OK", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right }
                }
            }
        };
        if (dialog.Content is StackPanel panel && panel.Children.LastOrDefault() is Button button)
            button.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(this);
    }

    private SceneObject? SelectedFolder => _session.SelectedObject is FolderObject folder ? folder : null;

    private void AddObject_Click(object? sender, RoutedEventArgs e) => _session.Add(SceneObjectKind.Object, SelectedFolder);
    private void AddImage_Click(object? sender, RoutedEventArgs e) => _session.Add(SceneObjectKind.Image, SelectedFolder);
    private void AddFolder_Click(object? sender, RoutedEventArgs e) => _session.Add(SceneObjectKind.Folder, SelectedFolder);
    private void AddSound_Click(object? sender, RoutedEventArgs e) => _session.Add(SceneObjectKind.Sound);
    private void AddSoundTrigger_Click(object? sender, RoutedEventArgs e) => _session.Add(SceneObjectKind.SoundTrigger, SelectedFolder);
    private void AddSpatialSound_Click(object? sender, RoutedEventArgs e) => _session.Add(SceneObjectKind.SpatialSoundTrigger, SelectedFolder);
    private void Delete_Click(object? sender, RoutedEventArgs e) => _session.DeleteSelected();
    private void Duplicate_Click(object? sender, RoutedEventArgs e) => _session.DuplicateSelected();
    private void Copy_Click(object? sender, RoutedEventArgs e) => _session.CopySelected();
    private void Paste_Click(object? sender, RoutedEventArgs e) => _session.Paste();
    private void Undo_Click(object? sender, RoutedEventArgs e) => _session.Undo();
    private void Redo_Click(object? sender, RoutedEventArgs e) => _session.Redo();
    private void Exit_Click(object? sender, RoutedEventArgs e) => Close();

    private enum UnsavedChoice
    {
        Save,
        Discard,
        Cancel
    }
}
