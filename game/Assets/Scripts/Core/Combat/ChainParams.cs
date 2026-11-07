namespace GMR.Core.Combat
{
    /// <summary>
    /// Chain modifiers for a chain leaving the hit target (AD-9, AD-10): extra jumps, extra radius and a falloff delta,
    /// all additive (default = no change). Starts at default for every request and is mutated only inside DamageSystem's
    /// modifier fold (AD-16); the result ends up in <see cref="DamageResult.Chain"/> for the status that chains.
    /// </summary>
    public struct ChainParams
    {
        public int MaxJumps;
        public float Radius;
        public float Falloff;

        public ChainParams(int maxJumps, float radius, float falloff)
        {
            MaxJumps = maxJumps;
            Radius = radius;
            Falloff = falloff;
        }

        public readonly bool IsNone => MaxJumps == 0 && Radius == 0f && Falloff == 0f;
    }
}
