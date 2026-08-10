using System.Collections.ObjectModel;
using TwoDCore.Core.Scene;

namespace TwoDCore.Editor.ViewModels;

public sealed class ExplorerNodeViewModel
{
    public ExplorerNodeViewModel(string name, string icon, SceneObject? item, IEnumerable<ExplorerNodeViewModel>? children = null)
    {
        Name = name;
        Icon = icon;
        Item = item;
        if (children != null) Children = new ObservableCollection<ExplorerNodeViewModel>(children);
    }

    public string Name { get; }
    public string Icon { get; }
    public SceneObject? Item { get; }
    public ObservableCollection<ExplorerNodeViewModel> Children { get; } = [];
}
