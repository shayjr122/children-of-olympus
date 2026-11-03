namespace GMR.Core.Controls
{
    /// <summary>
    /// Aim source (AD-7). Only MovementAim is implemented; RightStick is the FR-5 fallback hook, built only if SM-6
    /// fails (Story 9.6).
    /// </summary>
    public enum AimMode
    {
        MovementAim = 0,
        RightStick = 1,
    }
}
