using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    public enum PlayerActionType
    {
        PlaceDefense,
        PurchaseUpgrade,
        CraftItem
    }

    // A single undoable action taken during the Day phase. ResourceCost is
    // the total value that gets refunded if this action is undone; Spent lists
    // the exact materials consumed so undo can hand them back, and Created lists
    // exactly what the action added so undo can take it away.
    public class PlayerAction
    {
        public PlayerActionType Type { get; }
        public string Description { get; }
        public int ResourceCost { get; }
        public Point? Position { get; } // where a defense was placed, if applicable
        public IReadOnlyList<ItemStack> Spent { get; }
        public IReadOnlyList<ItemStack> Created { get; } // exactly what the action added, removed again on undo

        public PlayerAction(
            PlayerActionType type,
            string description,
            int resourceCost,
            Point? position = null,
            IReadOnlyList<ItemStack>? spent = null,
            IReadOnlyList<ItemStack>? created = null)
        {
            Type = type;
            Description = description;
            ResourceCost = resourceCost;
            Position = position;
            Spent = spent ?? Array.Empty<ItemStack>();
            Created = created ?? Array.Empty<ItemStack>();
        }
    }
}
