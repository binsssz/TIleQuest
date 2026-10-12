using System;
using System.Collections.Generic;

namespace TileQuest
{
    // One thing the food vendor sells: its price in Gold and how much energy
    // eating it restores.
    public class FoodItem
    {
        public string Name { get; }
        public int Price { get; }
        public int Energy { get; }

        public FoodItem(string name, int price, int energy)
        {
            Name = name;
            Price = price;
            Energy = energy;
        }
    }

    // The food vendor's menu. Like RecipeBook, it must stay sorted by Price
    // ascending with every Price unique, because binary search only works on
    // sorted data and returns one match per key.
    public static class FoodBook
    {
        public const string Currency = "Gold";

        public static readonly List<FoodItem> SortedByPrice = new()
        {
            new FoodItem("Bread", 1, 20),
            new FoodItem("Stew", 2, 40),
            new FoodItem("Roast Meat", 3, 65),
            new FoodItem("Pie", 4, 90),
        };

        // Binary search on the price tag: O(log n) instead of scanning the list.
        public static FoodItem? FindByPrice(int price)
        {
            int index = BinarySearchUtil.FindByKey(SortedByPrice, price, food => food.Price);
            return index >= 0 ? SortedByPrice[index] : null;
        }

        // Names are not the sort key, so a plain scan is the right tool.
        public static FoodItem? FindByName(string name)
        {
            foreach (FoodItem food in SortedByPrice)
            {
                if (string.Equals(food.Name, name, StringComparison.Ordinal))
                {
                    return food;
                }
            }
            return null;
        }
    }
}