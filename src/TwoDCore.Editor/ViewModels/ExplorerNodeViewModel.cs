using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using TwoDCore.Core.Scene;

namespace TwoDCore.Editor.ViewModels;

public sealed partial class ExplorerNodeViewModel : ObservableObject
{
    private static readonly Dictionary<string, Bitmap> IconCache = new(StringComparer.Ordinal);

    public ExplorerNodeViewModel(string name, string iconPath, SceneObject? item, IEnumerable<ExplorerNodeViewModel>? children = null, bool isExpanded = false)
    {
        Name = name;
        IconPath = iconPath;
        Item = item;
        _isExpanded = isExpanded;
        if (children != null) Children = new ObservableCollection<ExplorerNodeViewModel>(children);
    }

    public string Name { get; }
    public string IconPath { get; }
    public Bitmap Icon => GetIcon(IconPath);
    public SceneObject? Item { get; }
    public ObservableCollection<ExplorerNodeViewModel> Children { get; } = [];

    [ObservableProperty]
    private bool _isExpanded;

    private static Bitmap GetIcon(string path)
    {
        if (IconCache.TryGetValue(path, out Bitmap? cached)) return cached;
        using Stream stream = AssetLoader.Open(new Uri(path));
        Bitmap bitmap = new(stream);
        IconCache[path] = bitmap;
        return bitmap;
    }
}
