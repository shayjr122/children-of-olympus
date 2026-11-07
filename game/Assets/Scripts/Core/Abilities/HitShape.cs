namespace GMR.Core.Abilities
{
    /// <summary>
    /// How an ability finds its targets (AD-8); one Runtime hit resolver per shape. Box, Circle and Projectile ship in
    /// Epic 3; AllEnemies ships with Wrath of Zeus (Epic 4); ExpandingWave is reserved for Poseidon's Flood (Epic 8).
    /// </summary>
    public enum HitShape
    {
        Box = 0,
        Circle = 1,
        Projectile = 2,
        AllEnemies = 3,
        ExpandingWave = 4,
    }
}
