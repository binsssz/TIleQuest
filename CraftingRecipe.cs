using System.Collections.Generic;
using System.Linq;

namespace TileQuest
{
    // A weapon crafted by the Village Trader. Cost is the total market value
    // of its ingredients (values come from ItemCatalog); RecipeBook is sorted
    // by it for binary search.
    public class CraftingRecipe
    {
        public string Name { get; }
        public int Power { get; }
        public IReadOnlyList<ItemStack> Ingredients { get; }
        public int Cost { get; }

        public CraftingRecipe(string name, int power, params ItemStack[] ingredients)
        {
            Name = name;
            Power = power;
            Ingredients = ingredients;
            Cost = ingredients.Sum(ingredient => ingredient.Quantity * ItemCatalog.ValueOf(ingredient.Type));
        }
    }
}
