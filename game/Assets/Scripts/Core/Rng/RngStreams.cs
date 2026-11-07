namespace GMR.Core.Rng
{
    /// <summary>
    /// The only names RunRng streams may use (AD-2). Adding a random consumer means adding a name here,
    /// never reusing one. This file is on the BannedApiTests allow-list (AD-3).
    /// </summary>
    public static class RngStreams
    {
        public const string Waves = "waves";
        public const string Spawns = "spawns";
        public const string Ai = "ai";
        public const string Combat = "combat";
        public const string Status = "status";
        public const string Loot = "loot";
        public const string Feedback = "feedback";
    }
}
