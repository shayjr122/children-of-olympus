using GMR.Core.Players;

namespace GMR.Core.Rage
{
    /// <summary>
    /// A Player's RAGE reached 100% (AD-11): once per fill (again only after it dropped below full). Raised only by that
    /// Player's RageMeter (AD-16). Telemetry pairs it with UltimateUsed to count wasted RAGE (FR-16).
    /// </summary>
    public readonly struct RageFull
    {
        public readonly PlayerId Player;

        public RageFull(PlayerId player)
        {
            Player = player;
        }
    }
}
