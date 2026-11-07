using System;
using System.Collections.Generic;

namespace GMR.Core.Rng
{
    /// <summary>
    /// One RunSeed, many independent named streams (AD-2). Each stream's seed is derived statelessly as
    /// SplitMix64(RunSeed ^ Fnv1a64(name)), so drawing from one stream never shifts another.
    /// </summary>
    public sealed class RunRng
    {
        readonly Dictionary<string, IRng> _streams = new Dictionary<string, IRng>(StringComparer.Ordinal);

        public RunRng(ulong runSeed)
        {
            Seed = runSeed;
        }

        public ulong Seed { get; }

        /// <summary>The stream for <paramref name="name"/> (use RngStreams constants). Same instance on every call.</summary>
        public IRng Stream(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Stream name must be non-empty.", nameof(name));
            if (!_streams.TryGetValue(name, out var stream))
            {
                stream = new Pcg32Rng(RngHash.StreamSeed(Seed, name));
                _streams.Add(name, stream);
            }
            return stream;
        }
    }
}
