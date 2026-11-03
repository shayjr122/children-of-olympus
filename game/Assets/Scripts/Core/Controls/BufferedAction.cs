namespace GMR.Core.Controls
{
    /// <summary>Actions whose presses go through the InputBuffer (FR-8). Abilities start using it in Epic 3.</summary>
    public enum BufferedAction
    {
        Dash = 0,
        Basic = 1,
        Special = 2,
        Signature = 3,
        Ultimate = 4,
        HeroSwap = 5,
    }
}
