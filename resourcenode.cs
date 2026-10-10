using Microsoft.Xna.Framework;

namespace TileQuest
{
    public enum ResourceType
    {
        Wood,
        Stone,
        Iron,
        Gold,
    }

    // One place that decides what each resource looks like in the inventory,
    // so harvesting, pickups and (later) crafting agree on names and values.
    public static class ResourceItems
    {
        public static Item Create(ResourceType type) => type switch
        {
            ResourceType.Wood => new Item("Wood", "Resource", 1, 2),
            ResourceType.Stone => new Item("Stone", "Resource", 1, 3),
            ResourceType.Iron => new Item("Iron", "Resource", 1, 6),
            _ => new Item("Gold", "Resource", 1, 8),
        };
    }

    // A harvestable object standing on one blocked tile: a stump gives wood,
    // a boulder gives stone. Each swing that lands on it yields one item and
    // knocks one hit off; when the hits run out the node is depleted (it is
    // removed from the map until RestoreResourceNodes brings it back).
    public sealed class ResourceNode
    {
        public Point Tile { get; }
        public PropKind Prop { get; }
        public ResourceType Type { get; }
        public int MaxHits { get; }
        public int HitsRemaining { get; private set; }
        public bool IsDepleted => HitsRemaining <= 0;

        private ResourceNode(Point tile, PropKind prop, ResourceType type, int hits)
        {
            Tile = tile;
            Prop = prop;
            Type = type;
            MaxHits = hits;
            HitsRemaining = hits;
        }

        // Null when the prop kind is not something the player can harvest.
        public static ResourceNode? ForProp(PropKind prop, Point tile) => prop switch
        {
            PropKind.Stump => new ResourceNode(tile, prop, ResourceType.Wood, hits: 3),
            PropKind.TallBoulder or PropKind.RoundBoulder or PropKind.BrownBoulder =>
                new ResourceNode(tile, prop, ResourceType.Stone, hits: 4),
            _ => null,
        };

        public Item Hit()
        {
            if (HitsRemaining > 0)
            {
                HitsRemaining--;
            }

            return ResourceItems.Create(Type);
        }

        public void Restore()
        {
            HitsRemaining = MaxHits;
        }
    }
}