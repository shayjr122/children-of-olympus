using System;
using GMR.Core.Combat;
using GMR.Core.Players;
using UnityEngine;

namespace GMR.Core.Abilities
{
    /// <summary>
    /// One slot's ability state machine (FR-9, AD-8): Ready -> Startup -> Active -> Recovery -> Ready, plus cooldown.
    /// Plain C#, ticked with scaled time by the Runtime AbilityController. Timings are snapshotted at start; cooldown
    /// starts when the ability is used and is always 0 in the Basic slot (slot-agnostic assets). DASH may cancel the
    /// Recovery only. Sole raiser of AbilityUsed and, in the Ultimate slot, UltimateUsed (AD-16). The Ultimate slot is
    /// only started through UltimateGate (RAGE gate).
    /// </summary>
    public sealed class AbilityRunner
    {
        readonly CombatEvents _events;
        float _activeStart;
        float _recoveryStart;
        float _end;
        float _cooldownEnd = float.NegativeInfinity;

        public AbilityRunner(PlayerId owner, AbilitySlot slot, AbilityDefinition definition, CombatEvents events)
        {
            Owner = owner;
            Slot = slot;
            Definition = definition != null ? definition : throw new ArgumentNullException(nameof(definition));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public PlayerId Owner { get; }
        public AbilitySlot Slot { get; }
        public AbilityDefinition Definition { get; }
        public AbilityPhase Phase { get; private set; } = AbilityPhase.Ready;

        /// <summary>Fire direction chosen at start (normalized).</summary>
        public Vector2 Direction { get; private set; } = Vector2.right;

        /// <summary>Scaled time of the last start; NaN before the first use.</summary>
        public float StartTime { get; private set; } = float.NaN;

        public bool IsBusy => Phase != AbilityPhase.Ready;

        /// <summary>Cooldown in seconds as used by this slot: the asset's value, or 0 in the Basic slot. Read live.</summary>
        public float EffectiveCooldown => EffectiveCooldownFor(Slot, Definition);

        /// <summary>The Basic slot forces cooldown 0 (FR-9); other slots use the asset's cooldown.</summary>
        public static float EffectiveCooldownFor(AbilitySlot slot, AbilityDefinition definition) =>
            slot == AbilitySlot.Basic || definition == null ? 0f : Math.Max(0f, definition.Cooldown);

        public float CooldownRemaining(float now) => Math.Max(0f, _cooldownEnd - now);

        /// <summary>True if Ready and off cooldown at <paramref name="now"/> (call <see cref="Tick"/> first).</summary>
        public bool CanStart(float now) => Phase == AbilityPhase.Ready && now >= _cooldownEnd;

        /// <summary>Starts the ability if legal and raises AbilityUsed. A zero direction falls back to the last one.</summary>
        public bool TryStart(float now, Vector2 position, Vector2 direction)
        {
            if (!CanStart(now)) return false;
            if (direction.sqrMagnitude > 1e-8f) Direction = direction.normalized;

            StartTime = now;
            _activeStart = now + Math.Max(0f, Definition.Startup);
            _recoveryStart = _activeStart + Math.Max(0f, Definition.Active);
            _end = _recoveryStart + Math.Max(0f, Definition.Recovery);
            _cooldownEnd = now + EffectiveCooldown;
            Phase = AbilityPhase.Startup;

            _events.RaiseAbilityUsed(new AbilityUsed(Owner, Slot, Definition, position, Direction, now));
            if (Slot == AbilitySlot.Ultimate) _events.RaiseUltimateUsed(new UltimateUsed(Owner, Definition, position, now));
            return true;
        }

        /// <summary>Advances phases to <paramref name="now"/> and reports the transitions that happened.</summary>
        public AbilityStep Tick(float now)
        {
            var step = AbilityStep.None;
            if (Phase == AbilityPhase.Startup && now >= _activeStart)
            {
                Phase = AbilityPhase.Active;
                step |= AbilityStep.EnteredActive;
            }
            if (Phase == AbilityPhase.Active && now >= _recoveryStart)
            {
                Phase = AbilityPhase.Recovery;
                step |= AbilityStep.ExitedActive;
            }
            if (Phase == AbilityPhase.Recovery && now >= _end)
            {
                Phase = AbilityPhase.Ready;
                step |= AbilityStep.Finished;
            }
            return step;
        }

        /// <summary>DASH cancel (FR-6): ends the Recovery early. Startup and Active cannot be cancelled.</summary>
        public bool CancelRecovery()
        {
            if (Phase != AbilityPhase.Recovery) return false;
            Phase = AbilityPhase.Ready;
            return true;
        }

        /// <summary>Back to Ready with no cooldown (Hero rebuild).</summary>
        public void Reset()
        {
            Phase = AbilityPhase.Ready;
            _cooldownEnd = float.NegativeInfinity;
        }
    }
}
