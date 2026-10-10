using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    // ====================================================================
    //  RESOURCE LAYOUT  -  edit this file to move harvestable nodes and
    //  dungeon pickups. Coordinates are tiles, (0, 0) = top-left of the map.
    // ====================================================================
    //
    //  Nodes   - blocked objects the player swings at: PropKind.Stump gives
    //            wood, any boulder kind gives stone.
    //  Pickups - iron and gold lying on the floor; they are collected by
    //            walking onto the tile.
    //
    //  TileMap checks every entry at startup. A spot that is blocked, on a
    //  travel tile or the arrival point, or listed twice is skipped with a
    //  "[Resources] WARNING" line in the console. Press F3 in game to see the
    //  tiles (nodes outlined like props, pickups in gold).
    internal static class ResourceLayout
    {
        public readonly record struct NodeSpot(Point Tile, PropKind Prop);
        public readonly record struct PickupSpot(Point Tile, ResourceType Type);

        private static readonly NodeSpot[] ForestNodes =
        {
            new(new Point(13, 17), PropKind.Stump),
            new(new Point(22, 18), PropKind.Stump),
            new(new Point(27, 17), PropKind.Stump),
            new(new Point(33, 19), PropKind.Stump),
            new(new Point(15, 16), PropKind.RoundBoulder),
            new(new Point(25, 19), PropKind.BrownBoulder),
            new(new Point(30, 18), PropKind.TallBoulder),
            new(new Point(31, 20), PropKind.RoundBoulder),
        };

        private static readonly PickupSpot[] DungeonPickups =
        {
            // Iron: along the main route.
            new(new Point(32, 10), ResourceType.Iron),
            new(new Point(31, 18), ResourceType.Iron),
            new(new Point(24, 21), ResourceType.Iron),
            new(new Point(10, 5), ResourceType.Iron),
            new(new Point(8, 13), ResourceType.Iron),
            // Gold: in the far rooms.
            new(new Point(4, 5), ResourceType.Gold),
            new(new Point(18, 19), ResourceType.Gold),
            new(new Point(4, 24), ResourceType.Gold),
            new(new Point(22, 25), ResourceType.Gold),
        };

        public static IReadOnlyList<NodeSpot> NodesFor(WorldMapType map) =>
            map == WorldMapType.Forest ? ForestNodes : Array.Empty<NodeSpot>();

        public static IReadOnlyList<PickupSpot> PickupsFor(WorldMapType map) =>
            map == WorldMapType.Dungeon ? DungeonPickups : Array.Empty<PickupSpot>();
    }
}