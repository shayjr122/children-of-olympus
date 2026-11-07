using System;
using GMR.Core.Combat;
using GMR.Core.Players;

namespace GMR.Core.Combo
{
    /// <summary>
    /// One Player's Combo (FR-13, FR-14, AD-11). +1 per hit that Player's abilities, Ultimate or statuses land (status
    /// ticks and chain jumps count fully; repeat hits on the same enemy count). Resets to 0 when no hit lands within the
    /// Combo Window or when the Player takes damage; avoided hits never reset it. Ticked with scaled dt. Reacts only to
    /// events whose source is its own Player. Sole raiser of ComboChanged and ComboReset (AD-16).
    /// </summary>
    public sealed class ComboCounter
    {
        readonly CombatEvents _events;
        float _remaining;

        public ComboCounter(PlayerId owner, ComboConfig config, CombatEvents events)
        {
            Owner = owner;
            Config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public PlayerId Owner { get; }
        public ComboConfig Config { get; }

        /// <summary>Session override of the config mode (pause-menu toggle); null uses ComboConfig.Mode.</summary>
        public ComboMode? ModeOverride { get; set; }

        public ComboMode Mode => ModeOverride ?? Config.Mode;
        public int Count { get; private set; }

        /// <summary>Window granted by the last hit, seconds (0 while Count is 0).</summary>
        public float Window { get; private set; }

        /// <summary>Seconds left before the Combo times out (0 while Count is 0).</summary>
        public float WindowRemaining => Count > 0 ? Math.Max(0f, _remaining) : 0f;

        /// <summary>Remaining window as 0..1 of the window granted by the last hit, for the HUD bar.</summary>
        public float WindowRemaining01 => Count > 0 && Window > 0f ? Math.Min(1f, Math.Max(0f, _remaining / Window)) : 0f;

        /// <summary>Counts <paramref name="e"/> if this Player is its source; returns true if counted.</summary>
        public bool Handle(in HitLanded e)
        {
            if (!e.Source.Is(Owner)) return false;
            OnHit();
            return true;
        }

        /// <summary>Resets on damage taken by this Player; other Players' damage is ignored.</summary>
        public void Handle(in PlayerDamaged e)
        {
            if (e.Player == Owner) OnDamaged();
        }

        /// <summary>A hit landed: Count+1 and the window restarts (its length from the new Count).</summary>
        public void OnHit()
        {
            Count++;
            Window = Config.WindowFor(Count, Mode);
            _remaining = Window;
            _events.RaiseComboChanged(new ComboChanged(Owner, Count, Window));
        }

        /// <summary>The Player took damage: an active Combo resets immediately.</summary>
        public void OnDamaged() => Reset(ComboResetReason.Damage);

        /// <summary>Advances the window by scaled <paramref name="dt"/>; times out when it runs out.</summary>
        public void Tick(float dt)
        {
            if (Count == 0 || !(dt > 0f)) return;
            _remaining -= dt;
            if (_remaining <= 1e-5f) Reset(ComboResetReason.Timeout);
        }

        /// <summary>Sets the Combo to 0 without an event (encounter restart).</summary>
        public void Clear()
        {
            Count = 0;
            Window = 0f;
            _remaining = 0f;
        }

        void Reset(ComboResetReason reason)
        {
            if (Count == 0) return;
            int final = Count;
            Clear();
            _events.RaiseComboReset(new ComboReset(Owner, final, reason));
        }
    }
}
