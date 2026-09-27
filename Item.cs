namespace TileQuest
{
    // A single stack of an item in the player's inventory (wood, stone,
    // tools, or a drop from combat).
    public class Item
    {
        public string Name { get; }
        public string Category { get; }
        public int Quantity { get; set; }
        public int Value { get; }

        public Item(string name, string category, int quantity, int value)
        {
            Name = name;
            Category = category;
            Quantity = quantity;
            Value = value;
        }
    }
}
