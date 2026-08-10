using TwoDCore.Core.Scene;

namespace TwoDCore.Core.Editing;

public sealed class SceneHistory
{
    private readonly Stack<SceneDocument> _undo = [];
    private readonly Stack<SceneDocument> _redo = [];

    public bool CanUndo => _undo.Count > 0;
    public bool CanRedo => _redo.Count > 0;

    public void Capture(SceneDocument scene)
    {
        _undo.Push(scene.DeepClone());
        _redo.Clear();
    }

    public SceneDocument? Undo(SceneDocument current)
    {
        if (!CanUndo) return null;
        _redo.Push(current.DeepClone());
        return _undo.Pop();
    }

    public SceneDocument? Redo(SceneDocument current)
    {
        if (!CanRedo) return null;
        _undo.Push(current.DeepClone());
        return _redo.Pop();
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}
