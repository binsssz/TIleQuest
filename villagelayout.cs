namespace TileQuest
{
    // ====================================================================
    //  VILLAGE LAYOUT  -  this is the file you edit to design the village.
    // ====================================================================
    //
    //  Every string below is ONE ROW of the map and every character is ONE
    //  TILE. The map is 36 tiles wide (x = 0..35) and 28 tiles tall
    //  (y = 0..27). x counts left -> right, y counts top -> bottom, and
    //  (0, 0) is the top-left tile. Use the ruler above/below the grid and the
    //  "y=" label on each row: the character under x=17 on the row labelled
    //  y=13 is the tile at (17, 13).
    //
    //  LEGEND
    //    .   grass            walkable
    //    ,   tall grass       walkable
    //    =   dirt road        walkable
    //    L   village lantern  walkable (drawn on top of stone paving)
    //    P   stone paving     walkable; beside a road, draws as a thin sidewalk
    //    F   flowers/plant    walkable decoration (place one per tile)
    //    G   garden bed       walkable soil tile with a small plant (join G
    //                         tiles to make a larger bed)
    //    R   pebble cluster   blocks movement where its sprite is drawn
    //    T   tree             blocks movement near its trunk; leave room for
    //                         its canopy (tree type varies by position)
    //    S   player spawn     walkable dirt tile - exactly one
    //    X   forest gate      walkable travel tile - exactly one, and it
    //                         must stay at x=18, y=27 (bottom edge, centre)
    //    C   church           solid lower walls block; roof can be walked behind
    //    1   house, type 1    solid lower walls block; roof can be walked behind
    //    2   house, type 2    walls block; side section has its own hitbox
    //    H   animated bonfire BLOCKED 2x2 footprint; stone under both tiles
    //    W   village well     BLOCKED footprint, 2 wide x 2 tall (the sprite is 3
    //                         tall, so its roof overhangs the row above)
    //
    //  PROPS - each is ONE letter on ONE tile and that tile is BLOCKED. The
    //  sprite stands on its tile and reaches up and sideways past it, so leave
    //  room above it. The player walks in front of or behind a prop depending
    //  on which side of its tile they are on. Place them on grass ('.') -
    //  not on roads, paving or inside a building footprint. The sprite is
    //  chosen by position, so a layout always looks the same. More props are
    //  one row each in PropCatalog.cs.
    //    B   tall boulder (grey)             O   round boulder (grey, size varies)
    //    M   brown boulder (size varies)     K   blue crystal
    //    D   dead tree                       U   bush (green or autumn)
    //    N   big bush (green or autumn)      A   anvil
    //    J   tree stump                      Q   barrel
    //    E   scarecrow
    //
    //  Not placed by hand: loose pebbles are sprinkled over bare '.' grass and
    //  reed clumps are drawn on every ',' tile (Game1.DrawVillageGroundDetails).
    //  Press F3 in game to show the building and prop hitboxes.
    //
    //  PLACING A BUILDING: write its letter over its WHOLE footprint, e.g. a
    //  church is a 6x6 square of C. The top-left corner is its position.
    //  Buildings may touch each other but may not overlap, and a block of the
    //  wrong size is reported as an error. Their footprint is automatically
    //  collision follows the solid wall sections, not the full roof silhouette.
    //  Only the bottom row of C and 1 is paved; the full H footprint is stone.
    //  P tiles beside a road draw as thin stone edging; other P tiles are full stone. The
    //  sprite is drawn larger (church 8x7 tiles,
    //  house 2 8x7, house 1 5x7),
    //  centred on the footprint and lined up with its bottom edge, so it
    //  overhangs the walkable tiles beside it and above it.
    //
    //  EDITING RULES: keep all 28 rows, keep every row exactly 36 characters,
    //  keep the quotes and the trailing comma, and use no spaces.
    //
    //  VALIDATION: the layout is checked every time the game starts (see
    //  VillageGenerator.Generate). Mistakes stop the game with a list of
    //  problems and coordinates, including a gate or road tile that can't be
    //  reached from the spawn. On success the console prints a
    //  "[VillageLayout] OK" line, plus a WARNING if only grass or tall grass
    //  is unreachable.
    public static class VillageLayout
    {
        public static readonly string[] Rows =
        {
        //          0         1         2         3
        //          012345678901234567890123456789012345
        /* y= 0 */ "....................................",
        /* y= 1 */ "..............T.........O.......O...",
        /* y= 2 */ "........O.O.................K.......",
        /* y= 3 */ "..B.O....O..M..CCCCCC..M..B..K.B.O..",
        /* y= 4 */ "...............CCCCCC...............",
        /* y= 5 */ "O......T.......CCCCCC............T.O",
        /* y= 6 */ "...............CCCCCC.....2222......",
        /* y= 7 */ "........1111...CCCCCC.....2222....O.",
        /* y= 8 */ "O.,,,...1111...CCCCCC.....2222...,..",
        /* y= 9 */ "..,,,...1111...P====P.B...2222...,B.",
        /* y=10 */ ".B......1111.P========P...2222......",
        /* y=11 */ "O.....N.1111.=L======L=P..2222..N...",
        /* y=12 */ ".PPPPPPP1111P===========PPPPPPPPPPPO",
        /* y=13 */ ".================HH================.",
        /* y=14 */ ".================HH================.",
        /* y=15 */ ".=================================..",
        /* y=16 */ ".PPPPPPPPPPP===========PPPPPPPPPPP..",
        /* y=17 */ ".......2222.P=L======L=P.1111.......",
        /* y=18 */ ".B.....2222.P==========P.1111.....B.",
        /* y=19 */ "..,,,..2222...P======PP..1111.,,,...",
        /* y=20 */ "O.,,,..2222..P======P....1111.,,,..O",
        /* y=21 */ ".......2222.P======PT....1111U......",
        /* y=22 */ ".O..N..2222UP======PPPPPP1111.N..O..",
        /* y=23 */ "......=======================P......",
        /* y=24 */ ".,,,..PPPPPPPP======PPPPPPPPP...D...",
        /* y=25 */ ".,,,,E......M.P=====P...WW.........O",
        /* y=26 */ ".,,,,.....O...P===S=P...WW.Q.A...O..",
        /* y=27 */ ".O....O.B..B..P===X=PB....Q....J..O.",
        //          012345678901234567890123456789012345
        //          0         1         2         3
        };
    }
}