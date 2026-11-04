using GMR.Core.Players;

namespace GMR.Core.Combo
{
    /// <summary>A Player's Combo grew (AD-11). Raised only by that Player's ComboCounter (AD-16).</summary>
    public readonly struct ComboChanged
    {
        public readonly PlayerId Player;
        public readonly int Count;

        /// <summary>Combo Window granted by this hit, seconds.</summary>
        public readonly float Window;

        public ComboChanged(PlayerId player, int count, float window)
        {
            Player = player;
            Count = count;
            Window = window;
        }
    }
}
