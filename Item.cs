using System;
using System.Collections.Generic;

namespace TileQuest
{
    // Which HUD row / inventory group an item belongs to. Armor or other gear
    // (none exists yet) would go under Weapon. Declaration order is the
    // display order of the groups.
    public enum ItemCategory
    {
        Weapon,
        Food,
        Resource,
    }

    // One item type's static definition. Name is the item's identifier
    // everywhere in the project (inventory keys, recipes, icons, banners).
    public sealed record ItemDefinition(string Name, ItemCategory Category, int Value);

    // A quantity of one item type. Used for recipe costs, undo records and
    // anything else that says "N of this".
    public readonly record struct ItemStack(string Type, int Quantity);

    // The ONE place that defines every item type's category and market value.
    // The order of the entries below is the default display order within each
    // category (e.g. wood, stone, iron, gold in the Resource row).
    public static class ItemCatalog
    {
        private static readonly ItemDefinition[] Definitions =
        {
            // Weapons (crafted gear). Value = total value of the recipe's
            // ingredients; DsaDemo checks this against RecipeBook.
            new("Wooden Sword", ItemCategory.Weapon, 8),
            new("Stone Sword", ItemCategory.Weapon, 13),
            new("Iron Sword", ItemCategory.Weapon, 22),
            new("Gold Blade", ItemCategory.Weapon, 46),

            // Food (price in Gold x 8, the value of one Gold).
            new("Bread", ItemCategory.Food, 8),
            new("Stew", ItemCategory.Food, 16),
            new("Roast Meat", ItemCategory.Food, 24),
            new("Pie", ItemCategory.Food, 32),

            // Resources, including enemy loot (Bone).
            new("Wood", ItemCategory.Resource, 2),
            new("Stone", ItemCategory.Resource, 3),
            new("Iron", ItemCategory.Resource, 6),
            new("Gold", ItemCategory.Resource, 8),
            new("Bone", ItemCategory.Resource, 3),
        };

        private static readonly Dictionary<string, ItemDefinition> ByName = BuildByName();
        private static readonly Dictionary<ItemCategory, ItemDefinition[]> ByCategory = BuildByCategory();

        public static IReadOnlyList<ItemCategory> Categories { get; } =
            new[] { ItemCategory.Weapon, ItemCategory.Food, ItemCategory.Resource };

        public static bool IsKnown(string name) => ByName.ContainsKey(name);

        // Throws for an unknown name: every item in the game must be defined above.
        public static ItemDefinition Get(string name)
        {
            if (!ByName.TryGetValue(name, out ItemDefinition? definition))
            {
                throw new ArgumentException($"Unknown item type '{name}'. Define it in ItemCatalog.", nameof(name));
            }
            return definition;
        }

        public static ItemCategory CategoryOf(string name) => Get(name).Category;

        public static int ValueOf(string name) => Get(name).Value;

        // Definitions of one category, in default display order.
        public static IReadOnlyList<ItemDefinition> InCategory(ItemCategory category) => ByCategory[category];

        private static Dictionary<string, ItemDefinition> BuildByName()
        {
            var map = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
            foreach (ItemDefinition definition in Definitions)
            {
                map.Add(definition.Name, definition);
            }
            return map;
        }

        private static Dictionary<ItemCategory, ItemDefinition[]> BuildByCategory()
        {
            var map = new Dictionary<ItemCategory, ItemDefinition[]>();
            foreach (ItemCategory category in Enum.GetValues<ItemCategory>())
            {
                var list = new List<ItemDefinition>();
                foreach (ItemDefinition definition in Definitions)
                {
                    if (definition.Category == category)
                    {
                        list.Add(definition);
                    }
                }
                map[category] = list.ToArray();
            }
            return map;
        }
    }

    // A stack of one item type on its way into the inventory (a harvest, a
    // loot drop, a pickup, a chest entry). Category and Value come from
    // ItemCatalog, so they can never disagree with the inventory.
    public class Item
    {
        public string Name { get; }
        public int Quantity { get; }
        public ItemCategory Category => ItemCatalog.CategoryOf(Name);
        public int Value => ItemCatalog.ValueOf(Name);

        public Item(string name, int quantity)
        {
            ItemCatalog.Get(name);   // fail fast on an undefined item
            Name = name;
            Quantity = quantity;
        }
    }
}
