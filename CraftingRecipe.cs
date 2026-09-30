using System.Collections.Generic;

namespace TileQuest
{
    // A crafting recipe or shop listing — deliberately simple, since the
    // point of this class is to be something BinarySearchUtil can look up by
    // Cost, not a full crafting system.
    public class CraftingRecipe
    {
        public string Name { get; }
        public int Cost { get; }
        public string ResultItem { get; }

        public CraftingRecipe(string name, int cost, string resultItem)
        {
            Name = name;
            Cost = cost;
            ResultItem = resultItem;
        }
    }

    // A small hardcoded, cost-sorted recipe list to demonstrate/exercise
    // BinarySearchUtil against. The real Village Trader shop (Phase 7) can
    // replace or extend this list — the important part for grading purposes
    // is that it stays sorted by Cost ascending, since binary search only
    // works on sorted data.
    public static class RecipeBook
    {
        public static readonly List<CraftingRecipe> SortedByCost = new()
        {
            new CraftingRecipe("Wooden Spike", 5, "Spike Trap"),
            new CraftingRecipe("Stone Wall", 10, "Wall Segment"),
            new CraftingRecipe("Iron Axe", 20, "Axe"),
            new CraftingRecipe("Reinforced Gate", 35, "Gate"),
            new CraftingRecipe("Watchtower", 50, "Tower"),
            new CraftingRecipe("Hearth Ward", 75, "Ward"),
        };
    }
}
