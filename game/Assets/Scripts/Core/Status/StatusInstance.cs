using GMR.Core.Combat;

namespace GMR.Core.Status
{
    /// <summary>One active status on one target (FR-18). Owned and mutated only by its <see cref="StatusContainer"/>.</summary>
    public sealed class StatusInstance
    {
        internal StatusInstance(StatusDefinition definition, SourceId source, int stacks, float now)
        {
            Definition = definition;
            Source = source;
            Stacks = stacks;
            Remaining = definition.Duration;
            AppliedAt = now;
            LastAppliedAt = now;
        }

        public StatusDefinition Definition { get; }

        /// <summary>Who applied it most recently (Combo/RAGE attribution, co-op).</summary>
        public SourceId Source { get; internal set; }

        public int Stacks { get; internal set; }

        /// <summary>Seconds left (scaled time).</summary>
        public float Remaining { get; internal set; }

        /// <summary>Seconds this instance has been ticked.</summary>
        public float Elapsed { get; internal set; }

        public float AppliedAt { get; }
        public float LastAppliedAt { get; internal set; }

        /// <summary>True once removed (expired, consumed or cleared); behaviours holding it must stop using it.</summary>
        public bool IsRemoved { get; internal set; }
    }
}
