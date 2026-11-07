using GMR.Core.Players;

namespace GMR.Core.Rage
{
    /// <summary>A Player's RAGE changed (gain, decay or consume; AD-11). Raised only by that Player's RageMeter (AD-16).</summary>
    public readonly struct RageChanged
    {
        public readonly PlayerId Player;
        public readonly float Value01;
        public readonly float Previous01;

        /// <summary>The gain source; null for decay and consume.</summary>
        public readonly RageSource? Source;

        public RageChanged(PlayerId player, float value01, float previous01, RageSource? source)
        {
            Player = player;
            Value01 = value01;
            Previous01 = previous01;
            Source = source;
        }
    }
}
