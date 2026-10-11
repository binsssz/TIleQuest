namespace TileQuest
{
    // Glue between the three DSA pieces the trader uses: the Inventory linked
    // list (materials and crafted gear), RecipeBook + BinarySearchUtil (finding
    // the recipe), and ActionHistory (the undo stack). Kept free of any
    // MonoGame code so DsaDemo can exercise it at startup.
    public static class Crafting
    {
        public const string GearCategory = "Gear";

        public static bool CanCraft(Inventory inventory, CraftingRecipe recipe)
        {
            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                if (inventory.CountOf(ingredient.ItemName) < ingredient.Quantity)
                {
                    return false;
                }
            }
            return true;
        }

        // Spends the ingredients, adds the gear to the inventory, and pushes
        // the transaction onto the undo stack. Returns false (changing
        // nothing) if the player cannot afford it.
        public static bool TryCraft(Inventory inventory, ActionHistory history, CraftingRecipe recipe)
        {
            if (!CanCraft(inventory, recipe))
            {
                return false;
            }

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                inventory.RemoveQuantity(ingredient.ItemName, ingredient.Quantity);
            }

            inventory.AddItem(new Item(recipe.Name, GearCategory, 1, recipe.Cost));
            history.Record(new PlayerAction(
                PlayerActionType.CraftItem,
                recipe.Name,
                recipe.Cost,
                spent: recipe.Ingredients));
            return true;
        }

        // Pops the most recent transaction, removes the gear it created, and
        // refunds the materials it spent. Returns null if there was nothing
        // to undo.
        public static PlayerAction? UndoLast(Inventory inventory, ActionHistory history)
        {
            PlayerAction? action = history.Undo();
            if (action == null)
            {
                return null;
            }

            if (action.Type == PlayerActionType.CraftItem)
            {
                inventory.RemoveItem(action.Description);
                foreach (RecipeIngredient spent in action.Spent)
                {
                    inventory.AddItem(new Item(spent.ItemName, spent.Category, spent.Quantity, spent.UnitValue));
                }
            }

            return action;
        }

        // The strongest crafted weapon still in inventory is equipped, so
        // undoing a craft automatically falls back to the next best one.
        public static CraftingRecipe? GetBestWeapon(Inventory inventory)
        {
            CraftingRecipe? best = null;
            foreach (Item item in inventory.Items)
            {
                if (item.Category != GearCategory)
                {
                    continue;
                }

                CraftingRecipe? recipe = RecipeBook.FindByName(item.Name);
                if (recipe == null)
                {
                    continue;
                }

                if (best == null || recipe.Power > best.Power)
                {
                    best = recipe;
                }
            }
            return best;
        }
    }
}
