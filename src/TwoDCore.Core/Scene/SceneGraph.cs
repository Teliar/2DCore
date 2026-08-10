namespace TwoDCore.Core.Scene;

public static class SceneGraph
{
    public static IEnumerable<SceneObject> Traverse(IEnumerable<SceneObject> roots)
    {
        foreach (SceneObject item in roots)
        {
            yield return item;
            foreach (SceneObject child in Traverse(item.Children)) yield return child;
        }
    }

    public static SceneObject? FindById(IEnumerable<SceneObject> roots, Guid id) =>
        Traverse(roots).FirstOrDefault(item => item.Id == id);

    public static SceneObject? FindParent(IEnumerable<SceneObject> roots, SceneObject target)
    {
        foreach (SceneObject root in roots)
        {
            SceneObject? parent = FindParentRecursive(root, target);
            if (parent != null) return parent;
        }
        return null;
    }

    public static bool IsDescendant(SceneObject parent, SceneObject candidate)
    {
        if (ReferenceEquals(parent, candidate)) return true;
        return parent.Children.Any(child => IsDescendant(child, candidate));
    }

    public static bool Remove(SceneDocument scene, SceneObject target)
    {
        if (scene.Objects.Remove(target)) return true;
        return scene.Objects.Any(root => RemoveRecursive(root, target));
    }

    public static bool Move(SceneDocument scene, SceneObject item, SceneObject? newParent)
    {
        if (item.Kind == SceneObjectKind.SoundService || (newParent != null && IsDescendant(item, newParent)) ||
            ReferenceEquals(FindParent(scene.Objects, item), newParent))
        {
            return false;
        }
        if (newParent?.Kind == SceneObjectKind.SoundService && item.Kind != SceneObjectKind.Sound) return false;
        if (item.Kind == SceneObjectKind.Sound && newParent?.Kind != SceneObjectKind.SoundService) return false;

        if (!Remove(scene, item)) return false;
        if (newParent == null) scene.Objects.Add(item);
        else newParent.Children.Add(item);
        return true;
    }

    public static string GetUniqueName(SceneDocument scene, string baseName)
    {
        HashSet<string> names = Traverse(scene.Objects).Select(item => item.Name).ToHashSet(StringComparer.Ordinal);
        if (!names.Contains(baseName)) return baseName;
        for (int index = 1; ; index++)
        {
            string candidate = $"{baseName}_{index}";
            if (!names.Contains(candidate)) return candidate;
        }
    }

    public static void AssignNewIds(SceneObject item)
    {
        item.Id = Guid.NewGuid();
        foreach (SceneObject child in item.Children) AssignNewIds(child);
    }

    private static SceneObject? FindParentRecursive(SceneObject parent, SceneObject target)
    {
        if (parent.Children.Contains(target)) return parent;
        foreach (SceneObject child in parent.Children)
        {
            SceneObject? found = FindParentRecursive(child, target);
            if (found != null) return found;
        }
        return null;
    }

    private static bool RemoveRecursive(SceneObject parent, SceneObject target)
    {
        if (parent.Children.Remove(target)) return true;
        return parent.Children.Any(child => RemoveRecursive(child, target));
    }
}
