using Microsoft.Xna.Framework;

namespace TileQuest
{
    public enum PlayerActionType
    {
        PlaceDefense,
        PurchaseUpgrade
    }

    // A single undoable action taken during the Day phase. ResourceCost is
    // what gets refunded if this action is undone.
    public class PlayerAction
    {
        public PlayerActionType Type { get; }
        public string Description { get; }
        public int ResourceCost { get; }
        public Point? Position { get; } // where a defense was placed, if applicable

        public PlayerAction(PlayerActionType type, string description, int resourceCost, Point? position = null)
        {
            Type = type;
            Description = description;
            ResourceCost = resourceCost;
            Position = position;
        }
    }
}
