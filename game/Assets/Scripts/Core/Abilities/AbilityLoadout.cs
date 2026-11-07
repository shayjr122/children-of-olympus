using System.Collections.Generic;
using GMR.Core.Combat;
using GMR.Core.Controls;
using GMR.Core.Players;
using UnityEngine;

namespace GMR.Core.Abilities
{
    /// <summary>
    /// A Hero's ability slots (FR-9, AD-6): one AbilityRunner per assigned slot. A Hero runs one ability at a time;
    /// presses made while busy (or while a slot is cooling down) stay in the InputBuffer and fire when legal (FR-8).
    /// The Ultimate slot has a runner too (ticked like the others; the HUD shows it through the RAGE meter) but is never started by
    /// <see cref="TryStartBuffered"/>: only UltimateGate starts it, when RAGE is full (FR-17).
    /// </summary>
    public sealed class AbilityLoadout
    {
        static readonly AbilitySlot[] PressOrder = { AbilitySlot.Basic, AbilitySlot.Special, AbilitySlot.Signature };

        readonly AbilityRunner[] _bySlot = new AbilityRunner[4];
        readonly List<AbilityRunner> _runners = new List<AbilityRunner>(4);

        public AbilityLoadout(PlayerId owner, CombatEvents events, AbilityDefinition basic, AbilityDefinition special,
            AbilityDefinition signature, AbilityDefinition ultimate = null)
        {
            Add(owner, events, AbilitySlot.Basic, basic);
            Add(owner, events, AbilitySlot.Special, special);
            Add(owner, events, AbilitySlot.Signature, signature);
            Add(owner, events, AbilitySlot.Ultimate, ultimate);
        }

        /// <summary>Runners of the assigned slots.</summary>
        public IReadOnlyList<AbilityRunner> Runners => _runners;

        /// <summary>The runner for <paramref name="slot"/>, or null if the slot is empty.</summary>
        public AbilityRunner Get(AbilitySlot slot) => _bySlot[(int)slot];

        /// <summary>The ability currently running (not Ready), or null.</summary>
        public AbilityRunner Current
        {
            get
            {
                for (int i = 0; i < _runners.Count; i++)
                {
                    if (_runners[i].IsBusy) return _runners[i];
                }
                return null;
            }
        }

        /// <summary>True during Startup or Active: a DASH press waits in the buffer (only Recovery is cancellable).</summary>
        public bool BlocksDash
        {
            get
            {
                var current = Current;
                return current != null && (current.Phase == AbilityPhase.Startup || current.Phase == AbilityPhase.Active);
            }
        }

        /// <summary>True while a running ability roots the caster (FR-1 rootsCaster).</summary>
        public bool RootsCaster
        {
            get
            {
                var current = Current;
                return current != null && current.Definition.RootsCaster;
            }
        }

        /// <summary>DASH cancel: ends the running ability's Recovery. False if nothing was in Recovery.</summary>
        public bool CancelRecovery()
        {
            var current = Current;
            return current != null && current.CancelRecovery();
        }

        /// <summary>
        /// Starts the first buffered, legal ability (Basic, then Special, then Signature) and returns its runner, or
        /// null. Nothing starts while an ability is running. A press for an empty slot is consumed and ignored; a
        /// press for a slot on cooldown stays buffered (it fires if the cooldown ends inside the buffer window).
        /// </summary>
        public AbilityRunner TryStartBuffered(InputBuffer buffer, float now, Vector2 position, Vector2 direction)
        {
            if (buffer == null || Current != null) return null;
            for (int i = 0; i < PressOrder.Length; i++)
            {
                var slot = PressOrder[i];
                var action = ToBufferedAction(slot);
                if (!buffer.IsBuffered(action, now)) continue;
                var runner = _bySlot[(int)slot];
                if (runner == null)
                {
                    buffer.TryConsume(action, now);
                    continue;
                }
                if (!runner.CanStart(now)) continue;
                buffer.TryConsume(action, now);
                if (runner.TryStart(now, position, direction)) return runner;
            }
            return null;
        }

        public void Reset()
        {
            for (int i = 0; i < _runners.Count; i++) _runners[i].Reset();
        }

        public static BufferedAction ToBufferedAction(AbilitySlot slot) => slot switch
        {
            AbilitySlot.Basic => BufferedAction.Basic,
            AbilitySlot.Special => BufferedAction.Special,
            AbilitySlot.Signature => BufferedAction.Signature,
            _ => BufferedAction.Ultimate,
        };

        void Add(PlayerId owner, CombatEvents events, AbilitySlot slot, AbilityDefinition definition)
        {
            if (definition == null) return;
            var runner = new AbilityRunner(owner, slot, definition, events);
            _bySlot[(int)slot] = runner;
            _runners.Add(runner);
        }
    }
}
