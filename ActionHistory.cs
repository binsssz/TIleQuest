using System.Collections.Generic;

namespace TileQuest
{
    // DSA note: Stack<PlayerAction> piece. A stack gives exactly "undo the
    // most recent thing" (LIFO) for free — placing three defenses and
    // undoing once removes the third one, not the first, which is the
    // behavior an undo key needs and a Queue would get backwards.
    public class ActionHistory
    {
        private readonly Stack<PlayerAction> _actions = new();

        public bool CanUndo => _actions.Count > 0;
        public int Count => _actions.Count;

        // Call when the player places a defense or purchases an upgrade.
        public void Record(PlayerAction action)
        {
            _actions.Push(action);
        }

        // Pops the most recent action so the caller can undo its effect
        // (remove the placed defense, refund ResourceCost). Returns null if
        // there's nothing left to undo.
        public PlayerAction? Undo()
        {
            return _actions.Count > 0 ? _actions.Pop() : null;
        }

        // Call at the start of each new Day phase — actions from a previous
        // day shouldn't be undoable once the day has moved on.
        public void ClearForNewDay()
        {
            _actions.Clear();
        }
    }
}
