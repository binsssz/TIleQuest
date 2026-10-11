using System.Collections.Generic;
using System.Linq;

namespace TileQuest
{
    // One material a recipe consumes. UnitValue is that item's market value,
    // so a recipe's Cost and the values stored in the inventory always agree.
    public readonly record struct RecipeIngredient(string ItemName, string Category, int Quantity, int UnitValue);

    // A weapon crafted by the Village Trader. Cost is the total market value
    // of its ingredients; RecipeBook is sorted by it for binary search.
    public class CraftingRecipe
    {
        public string Name { get; }
        public int Power { get; }
        public IReadOnlyList<RecipeIngredient> Ingredients { get; }
        public int Cost { get; }

        public CraftingRecipe(string name, int power, params RecipeIngredient[] ingredients)
        {
            Name = name;
            Power = power;
            Ingredients = ingredients;
            Cost = ingredients.Sum(ingredient => ingredient.Quantity * ingredient.UnitValue);
        }
    }
}
