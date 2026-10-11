namespace TileQuest
{
    public enum EnemyKind
    {
        Goblin,
        Skeleton,
    }

    // Order matches the sprite arrays in Game1 (Base, Warrior, Rogue, Mage),
    // so (int)variant indexes them directly.
    public enum SkeletonVariant
    {
        Base,
        Warrior,
        Rogue,
        Mage,
    }

    // One place for how the skeleton variants differ. The wave spawner scales
    // these by the night number; the dungeon skeletons use them directly.
    public static class SkeletonStats
    {
        public static float HealthScale(SkeletonVariant variant) => variant switch
        {
            SkeletonVariant.Warrior => 1.4f,   // tanky and a little slow
            SkeletonVariant.Rogue => 0.75f,    // fragile but quick
            SkeletonVariant.Mage => 0.6f,      // glass cannon
            _ => 1.0f,
        };

        public static float SpeedScale(SkeletonVariant variant) => variant switch
        {
            SkeletonVariant.Warrior => 0.85f,
            SkeletonVariant.Rogue => 1.3f,
            _ => 1.0f,
        };

        // Damage to the hearth per strike (goblins deal 3).
        public static int HearthDamage(SkeletonVariant variant) => variant switch
        {
            SkeletonVariant.Warrior => 5,
            SkeletonVariant.Rogue => 3,
            SkeletonVariant.Mage => 5,
            _ => 4,
        };
    }
}
