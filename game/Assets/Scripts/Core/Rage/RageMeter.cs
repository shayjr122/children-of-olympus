using System;
using GMR.Core.Combat;
using GMR.Core.Players;

namespace GMR.Core.Rage
{
    /// <summary>
    /// One Player's RAGE, 0..1 (FR-15, FR-16, AD-11). Gains come from hits, status ticks / chain jumps, Combo milestones
    /// and Perfect Dodges (amounts in RageConfig); every positive gain restarts the decay grace, even when clamped at
    /// full. After the grace RAGE decays at the configured rate, also when full, so full RAGE with no gains reaches 0 in
    /// grace + 1 / rate seconds. Ticked with scaled dt. Sole raiser of RageChanged and RageFull (AD-16).
    /// </summary>
    public sealed class RageMeter
    {
        const float FullEpsilon = 1e-4f;

        readonly CombatEvents _events;
        RageConfig _config;
        float _sinceGain;

        public RageMeter(PlayerId owner, RageConfig config, CombatEvents events)
        {
            Owner = owner;
            _config = config != null ? config : throw new ArgumentNullException(nameof(config));
            _events = events ?? throw new ArgumentNullException(nameof(events));
        }

        public PlayerId Owner { get; }

        /// <summary>The active Hero's RageConfig (replaced when the Hero changes).</summary>
        public RageConfig Config
        {
            get => _config;
            set => _config = value != null ? value : throw new ArgumentNullException(nameof(value));
        }

        /// <summary>Session override of RageConfig.DecayEnabled (pause-menu toggle); null uses the config.</summary>
        public bool? DecayOverride { get; set; }

        public bool DecayOn => DecayOverride ?? _config.DecayEnabled;
        public float Value01 { get; private set; }
        public bool IsFull => Value01 >= 1f;

        /// <summary>Scaled seconds since the last gain (or consume).</summary>
        public float SinceLastGain => _sinceGain;

        /// <summary>Adds the configured amount for <paramref name="source"/>.</summary>
        public bool Add(RageSource source) => Add(source, _config.AmountFor(source));

        /// <summary>
        /// Adds <paramref name="amount"/> (clamped to 1) and restarts the decay grace. Raises RageChanged if the value
        /// changed and RageFull when it reaches 1. Returns false for non-positive amounts (no gain, grace unchanged).
        /// </summary>
        public bool Add(RageSource source, float amount)
        {
            if (!(amount > 0f)) return false;
            _sinceGain = 0f;
            float previous = Value01;
            float next = previous + amount;
            if (next >= 1f - FullEpsilon) next = 1f; // 50 x 0.02f sums to 0.9999996f: snap so the meter really fills
            if (next == previous) return true;
            Value01 = next;
            _events.RaiseRageChanged(new RageChanged(Owner, next, previous, source));
            if (next >= 1f && previous < 1f) _events.RaiseRageFull(new RageFull(Owner));
            return true;
        }

        /// <summary>Empties the meter (Ultimate fired).</summary>
        public void Consume()
        {
            _sinceGain = 0f;
            float previous = Value01;
            if (previous <= 0f) return;
            Value01 = 0f;
            _events.RaiseRageChanged(new RageChanged(Owner, 0f, previous, null));
        }

        /// <summary>Advances the decay clock by scaled <paramref name="dt"/>; decays only past the grace.</summary>
        public void Tick(float dt)
        {
            if (!(dt > 0f)) return;
            float before = _sinceGain;
            _sinceGain += dt;
            if (Value01 <= 0f || !DecayOn) return;
            float decayTime = _sinceGain - Math.Max(before, _config.DecayGrace);
            if (!(decayTime > 0f) || !(_config.DecayRate > 0f)) return;
            float previous = Value01;
            Value01 = Math.Max(0f, previous - _config.DecayRate * decayTime);
            _events.RaiseRageChanged(new RageChanged(Owner, Value01, previous, null));
        }

        /// <summary>Sets RAGE to 0 without an event (encounter restart).</summary>
        public void Clear()
        {
            Value01 = 0f;
            _sinceGain = 0f;
        }
    }
}
