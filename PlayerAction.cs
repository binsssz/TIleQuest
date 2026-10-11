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
    // the exact materials consumed so undo can hand them back.
    public class PlayerAction
    {
        public PlayerActionType Type { get; }
        public string Description { get; }
        public int ResourceCost { get; }
        public Point? Position { get; } // where a defense was placed, if applicable
        public IReadOnlyList<RecipeIngredient> Spent { get; }

        public PlayerAction(
            PlayerActionType type,
            string description,
            int resourceCost,
            Point? position = null,
            IReadOnlyList<RecipeIngredient>? spent = null)
        {
            Type = type;
            Description = description;
            ResourceCost = resourceCost;
            Position = position;
            Spent = spent ?? Array.Empty<RecipeIngredient>();
        }
    }
}
