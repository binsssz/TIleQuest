using System;
using System.Collections.Generic;

namespace TileQuest
{
    // The trader's recipe list. It must stay sorted by Cost ascending, with
    // every Cost unique, because binary search only works on sorted data and
    // returns one match per key.
    public static class RecipeBook
    {
        // Ingredient helpers; each material's market value comes from ItemCatalog.
        private static ItemStack Wood(int quantity) => new("Wood", quantity);
        private static ItemStack Stone(int quantity) => new("Stone", quantity);
        private static ItemStack Iron(int quantity) => new("Iron", quantity);
        private static ItemStack Gold(int quantity) => new("Gold", quantity);

        public static readonly List<CraftingRecipe> SortedByCost = new()
        {
            new CraftingRecipe("Wood Sword", 3, Wood(4)),                       
            new CraftingRecipe("Stone Sword", 6, Wood(2), Stone(3)),              
            new CraftingRecipe("Iron Sword", 9, Wood(2), Iron(3)),
            new CraftingRecipe("Gold Blade", 14, Wood(2), Iron(3), Gold(3)),     
        };

        // Binary search on the price tag: O(log n) instead of scanning the list.
        public static CraftingRecipe? FindByCost(int cost)
        {
            int index = BinarySearchUtil.FindByKey(SortedByCost, cost, recipe => recipe.Cost);
            return index >= 0 ? SortedByCost[index] : null;
        }

        // Only a handful of recipes and names are not the sort key, so a plain
        // scan is the right tool here.
        public static CraftingRecipe? FindByName(string name)
        {
            foreach (CraftingRecipe recipe in SortedByCost)
            {
                if (string.Equals(recipe.Name, name, StringComparison.Ordinal))
                {
                    return recipe;
                }
            }
            return null;
        }
    }
}
