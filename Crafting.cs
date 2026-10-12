namespace TileQuest
{
    // Glue between the three DSA pieces the trader uses: the Dictionary
    // Inventory (materials and crafted gear), RecipeBook + BinarySearchUtil
    // (finding the recipe), and ActionHistory (the undo stack). Kept free of
    // any MonoGame code so DsaDemo can exercise it at startup.
    public static class Crafting
    {
        public static bool CanCraft(Inventory inventory, CraftingRecipe recipe)
        {
            foreach (ItemStack ingredient in recipe.Ingredients)
            {
                if (!inventory.Has(ingredient.Type, ingredient.Quantity))
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
            // TrySpend is atomic: it checks every ingredient before deducting
            // any, so a failed craft never leaves a half-paid transaction.
            if (!inventory.TrySpend(recipe.Ingredients))
            {
                return false;
            }

            var created = new[] { new ItemStack(recipe.Name, 1) };
            foreach (ItemStack stack in created)
            {
                inventory.Add(stack.Type, stack.Quantity);
            }

            history.Record(new PlayerAction(
                PlayerActionType.CraftItem,
                recipe.Name,
                recipe.Cost,
                spent: recipe.Ingredients,
                created: created));
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
                // Remove exactly what the craft created (if the player already
                // dropped it, there is nothing to take back), then hand back
                // exactly what it spent.
                foreach (ItemStack created in action.Created)
                {
                    inventory.TryRemove(created.Type, created.Quantity);
                }
                foreach (ItemStack spent in action.Spent)
                {
                    inventory.Add(spent.Type, spent.Quantity);
                }
            }

            return action;
        }

        // The strongest crafted weapon still in inventory is equipped, so
        // undoing a craft automatically falls back to the next best one.
        public static CraftingRecipe? GetBestWeapon(Inventory inventory)
        {
            CraftingRecipe? best = null;
            foreach ((string type, int qty) in inventory.GetItems(ItemCategory.Weapon))
            {
                CraftingRecipe? recipe = RecipeBook.FindByName(type);
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
