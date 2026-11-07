namespace GMR.Core.Combo
{
    /// <summary>How the Combo Window is chosen (FR-13, FR-14). Toggleable in the pause menu.</summary>
    public enum ComboMode
    {
        /// <summary>The window is always ComboConfig.FixedWindow.</summary>
        Fixed = 0,

        /// <summary>The window follows ComboConfig.ShrinkingCurve over the Combo count, never below MinWindow.</summary>
        Shrinking = 1,
    }
}
