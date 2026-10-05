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
    //  THE IDEA (stacked version): three staggered terraces climb the map - a wide
    //  top tier (y0-6) with two caves, a left tier (y8-12), and a right tier
    //  (y14-19) with a third cave. Each tier is separated from the next by ONE
    //  empty row, so the roots/grass fringe under the upper tier tucks behind
    //  the lower tier's top edge. That overlap is what makes it read as one tall
    //  hill. The ledge rows between tiers are walkable but mostly hidden by the
    //  fringe; give a gap 3 rows if you want a visible path along it.
    //
    //  LEGEND
    //  -- ground / walkable ------------------------------------------------
    //    .   grass            walkable
    //    ,   tall grass       walkable (fern/reed clumps drawn on it)
    //    =   dirt             walkable (clearing + trail)
    //    X   village gate     walkable travel tile - exactly one, fixed at
    //                         x=18, y=27. You arrive on x=18, y=26.
    //    F   flowers          walkable decoration (small ground flowers)
    //    I   foxglove         walkable decoration (tall purple flower)
    //    m   mushrooms        walkable decoration (orange caps)
    //
    //  -- the hill ---------------------------------------------------------
    //    #   hill rock        BLOCKED. Write # over every tile of the hill;
    //                         the piece drawn is picked automatically from
    //                         the neighbours. Sprite: the brown set in
    //                         Environment/Tilesets/Wall_Tiles.png, cut into
    //                         16px cells (column, row), x = col*16, y = row*16:
    //                           top row of a block ........ row 0
    //                           row under the top ......... row 1
    //                           plateau middle ............ rows 2-4
    //                           bottom of plateau ......... row 5
    //                           cliff face ................ rows 6-7
    //                         Columns: left edge col 0, right edge col 5,
    //                         everything between uses cols 1-4.
    //                         The two rows under the BOTTOM row of # are a
    //                         walkable grass fringe (sheet rows 8-9), added
    //                         automatically.
    //                         Rules: a block of # must be 5+ rows tall and
    //                         leave 1+ empty row between it and a block
    //                         below it (the fringe sits there). Draw order
    //                         is top tier first, lowest tier last.
    //    V   cave pit         BLOCKED, a solid 6 wide x 6 tall square of V,
    //                         always inside # rock. Sprite: same sheet,
    //                         source (0, 304, 96, 96), drawn over the rock so
    //                         the rim looks carved in. The row below its
    //                         bottom edge is the cave mouth.
    //
    //  -- props (one letter, one tile; see PropCatalog.cs) ----------------
    //    T   tree             blocks near its trunk; keep trees 2+ tiles
    //                         apart sideways and 4+ up/down.
    //    D   dead tree        U   bush            N   big bush
    //    B   tall boulder     O   round boulder   M   brown boulder
    //    K   blue crystal     J   tree stump
    //
    //  NEW catalog rows needed (Vegetation/Vegetation.png):
    //    I  foxglove  (194,160,13,33)  (211,164,11,29)  (228,175,10,18)
    //    m  mushroom  (48,337,16,16)   (35,339,10,11)   (20,340,7,9)
    //
    //  EDITING RULES: keep all 28 rows, each exactly 36 characters, keep
    //  the quotes and trailing comma, and use no spaces. Keep the trail
    //  (x=12..13, y=13..22) and the gate corridor clear so everything stays
    //  reachable from the gate.
    public static class ForestLayout
    {
        public static readonly string[] Rows =
        {
        //          0         1         2         3
        //          012345678901234567890123456789012345
        /* y= 0 */ ".m..############################....",
        /* y= 1 */ "..m.####VVVVVV########VVVVVV####....",
        /* y= 2 */ "....####VVVVVV########VVVVVV####.D..",
        /* y= 3 */ "..T.####VVVVVV########VVVVVV####..D.",
        /* y= 4 */ "....####VVVVVV########VVVVVV####....",
        /* y= 5 */ "T...####VVVVVV########VVVVVV####....",
        /* y= 6 */ "..D.####VVVVVV########VVVVVV####....",
        /* y= 7 */ "..........m.m..K....m........mN.....",
        /* y= 8 */ "....###################.............",
        /* y= 9 */ "T...###################....I.I......",
        /* y=10 */ "...D###################...I.I.......",
        /* y=11 */ "....###################....I......D.",
        /* y=12 */ "..,,###################.....O.......",
        /* y=13 */ "T.....m.K.m.==..,....m.....B...U....",
        /* y=14 */ "............==.#########VVVVVV####..",
        /* y=15 */ "..,,........==.#########VVVVVV####..",
        /* y=16 */ "...,....N...==.#########VVVVVV####..",
        /* y=17 */ "T.........O.==.#########VVVVVV####..",
        /* y=18 */ "......U.....==.#########VVVVVV####..",
        /* y=19 */ "..T.........==.#########VVVVVV####..",
        /* y=20 */ "............==U..m......K...m.m.....",
        /* y=21 */ "T...T...F...==========.........,,...",
        /* y=22 */ ".....,,...==============F.,.....J...",
        /* y=23 */ "..T....O.===============....,.O...T.",
        /* y=24 */ ".........==============F...M........",
        /* y=25 */ "T...T.N...F===========..........,,..",
        /* y=26 */ ".......,,...II.I.===.F...,F.........",
        /* y=27 */ "...........I..I...X....,............",
        //          012345678901234567890123456789012345
        //          0         1         2         3
        };
    }
}