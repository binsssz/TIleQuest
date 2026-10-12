namespace TileQuest
{
    public enum EatResult
    {
        Eaten,
        NotFood,
        NotOwned,
        AlreadyFull,
    }

    // Glue between the food vendor (FoodBook), the Inventory linked list, and
    // PlayerStats. Kept free of any MonoGame code so DsaDemo can exercise it
    // at startup. Purchases are final: food can be eaten, so they are not
    // pushed onto the undo stack.
    public static class FoodShop
    {
        public static bool CanAfford(Inventory inventory, FoodItem food)
        {
            return inventory.CountOf(FoodBook.Currency) >= food.Price;
        }

        // Spends the gold and adds one food item to the bag. Returns false
        // (changing nothing) if the player cannot afford it.
        public static bool TryBuy(Inventory inventory, FoodItem food)
        {
            if (!CanAfford(inventory, food) || !inventory.RemoveQuantity(FoodBook.Currency, food.Price))
            {
                return false;
            }

            inventory.AddItem(new Item(food.Name, FoodBook.Category, 1, food.Price * FoodBook.GoldUnitValue));
            return true;
        }

        // Eats one unit of the named food: removes it from the bag and
        // restores its energy. Nothing is consumed if the item is not food,
        // is not owned, or energy is already full.
        public static EatResult TryEat(Inventory inventory, PlayerStats stats, string itemName, string category)
        {
            if (category != FoodBook.Category || FoodBook.FindByName(itemName) is not FoodItem food)
            {
                return EatResult.NotFood;
            }

            if (stats.Energy >= PlayerStats.MaxEnergy)
            {
                return EatResult.AlreadyFull;
            }

            if (!inventory.RemoveOne(itemName, category))
            {
                return EatResult.NotOwned;
            }

            stats.RestoreEnergy(food.Energy);
            return EatResult.Eaten;
        }
    }
}