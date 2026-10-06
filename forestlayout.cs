namespace TileQuest
{
    // ====================================================================
    //  FOREST LAYOUT  -  edit this file to design the forest map.
    // ====================================================================
    //
    //  Same rules as VillageLayout.cs: every string is ONE ROW, every
    //  character is ONE TILE. The map is 36 wide (x = 0..35) and 28 tall
    //  (y = 0..27); (0, 0) is the top-left tile.
    //
    //  LEGEND
    //    .   grass            walkable
    //    ,   tall grass       walkable; fern/reed clumps are drawn on it
    //    =   dirt path        walkable
    //    F   flowers/plant    walkable decoration (one plant per tile)
    //    I   foxglove         walkable decoration (tall purple flower)
    //    m   mushrooms        walkable decoration
    //    X   village gate     walkable travel tile; exactly one at x=18, y=14.
    //                         This is also the forest arrival tile.
    //
    //  TREE MODELS - each letter places that exact model on one blocked tile.
    //  The image is chosen from its green variant row:
    //    a   tree model 1, top row
    //    b   tree model 2, top row
    //    c   tree model 3, top row
    //    d   tree model 4, bottom row
    //    e   tree model 5, bottom row
    //    f   tree model 6, bottom row
    //    g   tree model 7, top row
    //    h   tree model 8, top row
    //
    //  PROPS - each is one letter on one blocked tile; sprites can extend up
    //  and sideways beyond their tile, so leave space around them.
    //    B   tall boulder (grey)             O   round boulder (grey)
    //    M   brown boulder                   K   blue crystal
    //    D   dead tree                       U   bush (green or autumn)
    //    N   big bush (green or autumn)      J   tree stump
    //    A   anvil                           Q   barrel
    //    E   scarecrow
    //
    //  TERRAIN
    //    #   hill rock        blocked cliff/foothill tile
    //    V   cave pit         blocked, solid 6x6 block inside hill rock
    //
    //  Village-only symbols from VillageLayout.cs (L, P, G, R, S, C, 1, 2,
    //  H and W) are not used in this forest layout.
    //
    //  EDITING RULES: keep all 28 rows exactly 36 characters wide, keep the
    //  quotes and trailing comma, and use no spaces. Leave x=18, y=14 as X.
    //
    //  Keep all 28 rows exactly 36 characters wide.
    public static class ForestLayout
    {
        public static readonly string[] Rows =
        {
        //          0         1         2         3
        //          012345678901234567890123456789012345
        /* y= 0 */ "....................................",
        /* y= 1 */ "....................................",
        /* y= 2 */ "....................................",
        /* y= 3 */ "....................................",
        /* y= 4 */ "....................................",
        /* y= 5 */ "....................................",
        /* y= 6 */ "....................................",
        /* y= 7 */ "....................................",
        /* y= 8 */ "....................................",
        /* y= 9 */ "....................................",
        /* y=10 */ "....................................",
        /* y=11 */ "....................................",
        /* y=12 */ "....................................",
        /* y=13 */ "....................................",
        /* y=14 */ "..................X.................",
        /* y=15 */ "....................................",
        /* y=16 */ "....................................",
        /* y=17 */ "....................................",
        /* y=18 */ "....................................",
        /* y=19 */ "....................................",
        /* y=20 */ "....................................",
        /* y=21 */ "....................................",
        /* y=22 */ "....................................",
        /* y=23 */ "##########..........................",
        /* y=24 */ "############........................",
        /* y=25 */ "############........................",
        /* y=26 */ "f..e................................",
        /* y=27 */ "..c.h...............................",
        //          012345678901234567890123456789012345
        //          0         1         2         3
        };
    }
}