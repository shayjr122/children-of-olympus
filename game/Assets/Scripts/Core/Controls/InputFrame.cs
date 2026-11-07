using UnityEngine;

namespace GMR.Core.Controls
{
    /// <summary>
    /// One Player's input for one frame (AD-7). Filled only by PlayerInputReader; gameplay reads it and never
    /// touches InputAction. "Pressed" flags are true only on the frame the button went down.
    /// </summary>
    public struct InputFrame
    {
        public Vector2 Move;
        public Vector2 Aim;

        public bool DashPressed;
        public bool BasicPressed;
        public bool BasicHeld;
        public bool SpecialPressed;
        public bool SignaturePressed;
        public bool UltimatePressed;
        public bool DirectionLockHeld;
        public bool HeroSwapPressed;
        public bool PausePressed;

        /// <summary>Debug map ToggleDebugMenu (Select / F1). Opens the same pause/debug menu as Pause.</summary>
        public bool DebugMenuPressed;

        /// <summary>Debug map ApplyWetToAll (D-pad up / F2): DevTools applies Wet to every enemy.</summary>
        public bool DebugApplyWetPressed;

        /// <summary>True if any one-frame press flag is set.</summary>
        public readonly bool AnyPressed =>
            DashPressed || BasicPressed || SpecialPressed || SignaturePressed ||
            UltimatePressed || HeroSwapPressed || PausePressed || DebugMenuPressed || DebugApplyWetPressed;

        /// <summary>Clears the one-frame press flags; axes and held states are kept.</summary>
        public void ClearPresses()
        {
            DashPressed = false;
            BasicPressed = false;
            SpecialPressed = false;
            SignaturePressed = false;
            UltimatePressed = false;
            HeroSwapPressed = false;
            PausePressed = false;
            DebugMenuPressed = false;
            DebugApplyWetPressed = false;
        }
    }
}
