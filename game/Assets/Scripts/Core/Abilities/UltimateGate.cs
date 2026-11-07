using System;
using GMR.Core.Combat;
using GMR.Core.Players;
using GMR.Core.Rage;
using UnityEngine;

namespace GMR.Core.Abilities
{
    /// <summary>
    /// RAGE gate of the Ultimate slot (FR-17, AD-8): an Ultimate is an ordinary AbilityDefinition that may only start
    /// when its Player's RAGE is full. On fire the Ultimate's AbilityRunner starts (it raises AbilityUsed and
    /// UltimateUsed), an UltimateStartup invulnerability window covering the startup is registered on the Hero's
    /// tracker (AD-16) and RAGE is consumed to 0. Below full nothing fires and UltimateNotReady is raised (its owner).
    /// </summary>
    public sealed class UltimateGate
    {
        readonly CombatEvents _events;

        public UltimateGate(PlayerId owner, CombatEvents events)
        {
            Owner = owner;
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public PlayerId Owner { get; }

        /// <summary>
        /// Handles one Ultimate press at scaled time <paramref name="now"/>. <paramref name="heroBusy"/> is true while
        /// another ability runs or the Hero is dashing (a full-RAGE press then waits in the buffer).
        /// </summary>
        public UltimateGateResult TryFire(RageMeter rage, AbilityRunner ultimate, InvulnerabilityTracker invulnerability,
            bool heroBusy, float now, Vector2 position, Vector2 direction)
        {
            if (rage == null) throw new ArgumentNullException(nameof(rage));
            if (rage.Owner != Owner) throw new ArgumentException($"RageMeter of {rage.Owner} passed to the gate of {Owner}.");
            if (ultimate == null) return UltimateGateResult.NoUltimate;
            if (ultimate.Slot != AbilitySlot.Ultimate)
                throw new ArgumentException($"Runner in slot {ultimate.Slot} is not an Ultimate.", nameof(ultimate));

            // Busy first: the press is judged against RAGE only once the Hero can act on it.
            if (heroBusy || !ultimate.CanStart(now)) return UltimateGateResult.Waiting;

            if (!rage.IsFull)
            {
                _events.RaiseUltimateNotReady(new UltimateNotReady(Owner, rage.Value01, position, now));
                return UltimateGateResult.NotReady;
            }

            // Consume and register i-frames before starting, so AbilityUsed/UltimateUsed subscribers already see
            // RAGE at 0 and the Hero invulnerable. CanStart was checked above, so TryStart succeeds.
            rage.Consume();
            invulnerability?.Register(now, Math.Max(0f, ultimate.Definition.Startup), InvulnerabilitySource.UltimateStartup);
            ultimate.TryStart(now, position, direction);
            return UltimateGateResult.Fired;
        }
    }
}
