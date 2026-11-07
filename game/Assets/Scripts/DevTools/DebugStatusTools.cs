using GMR.Core.Combat;
using GMR.Core.Players;
using GMR.Core.Status;
using GMR.Runtime.Encounter;
using UnityEngine;

namespace GMR.DevTools
{
    /// <summary>
    /// Debug status tools (Story 5.3): "Apply Wet to all" applies the Wet status to every living enemy that can carry
    /// statuses, attributed to the Player who asked. Triggered by any Player's Debug "ApplyWetToAll" press (D-pad up / F2,
    /// read through PlayerInputReader) or the pause-menu button (Player 0). Stands in for Theseus until Epic 8.
    /// Applies through each target's StatusContainer (the only status owner); Wet itself deals no damage.
    /// </summary>
    public sealed class DebugStatusTools : MonoBehaviour
    {
        [SerializeField] EncounterBootstrap _bootstrap;
        [SerializeField, Tooltip("Status_Wet.")] StatusDefinition _wet;
        [SerializeField, Tooltip("Pause-menu button (optional).")] UnityEngine.UI.Button _applyWetButton;

        /// <summary>How many enemies the last "Apply Wet to all" reached.</summary>
        public int LastAppliedCount { get; private set; }

        void OnEnable()
        {
            if (_bootstrap == null || _wet == null)
            {
                UnityEngine.Debug.LogError("[GMR/DevTools] DebugStatusTools needs a bootstrap and the Wet status.", this);
                enabled = false;
                return;
            }
            if (_applyWetButton != null) _applyWetButton.onClick.AddListener(ApplyWetFromMenu);
        }

        void OnDisable()
        {
            if (_applyWetButton != null) _applyWetButton.onClick.RemoveListener(ApplyWetFromMenu);
        }

        void Update()
        {
            if (Time.timeScale <= 0f) return; // paused: D-pad up is menu navigation (use the menu button)
            var players = _bootstrap.PlayerRoots;
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].Frame.DebugApplyWetPressed) ApplyWetToAll(players[i].Id);
            }
        }

        /// <summary>Menu button: applies Wet for Player 0.</summary>
        public void ApplyWetFromMenu() => ApplyWetToAll(new PlayerId(0));

        /// <summary>Applies Wet to every living enemy with a StatusContainer; returns how many.</summary>
        public int ApplyWetToAll(PlayerId by)
        {
            if (_bootstrap == null || _wet == null) return 0;
            var context = _bootstrap.Context;
            var enemies = context.Enemies.All;
            var source = SourceId.ForPlayer(by);
            int count = 0;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (!(enemies[i] is IDamageable target) || !target.IsAlive || target.Statuses == null) continue;
                target.Statuses.Apply(_wet, 1, source, Time.time);
                count++;
            }
            LastAppliedCount = count;
            UnityEngine.Debug.Log($"[GMR/DevTools] Apply Wet to all: {count} enemies (by {source}).");
            return count;
        }
    }
}
