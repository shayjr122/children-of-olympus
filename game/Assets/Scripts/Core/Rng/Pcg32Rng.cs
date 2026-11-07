using System;

namespace GMR.Core.Rng
{
    /// <summary>
    /// PCG32 (XSH-RR, 64-bit state) stream. Hand-written so sequences are identical on every runtime (AD-2).
    /// </summary>
    public sealed class Pcg32Rng : IRng
    {
        const ulong Multiplier = 6364136223846793005UL;
        const float Inv24 = 1f / 16777216f;

        readonly ulong _seed;
        readonly ulong _inc;
        ulong _state;

        public Pcg32Rng(ulong seed)
        {
            _seed = seed;
            unchecked
            {
                ulong seq = RngHash.SplitMix64(seed);
                _inc = (seq << 1) | 1UL;
                _state = 0UL;
                NextUInt();
                _state += seed;
                NextUInt();
            }
        }

        /// <summary>The seed this stream was built from (used for stateless Derive).</summary>
        public ulong Seed => _seed;

        public uint NextUInt()
        {
            unchecked
            {
                ulong old = _state;
                _state = old * Multiplier + _inc;
                uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
                int rot = (int)(old >> 59);
                return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
            }
        }

        public float Value01() => (NextUInt() >> 8) * Inv24;

        public int Range(int minIncl, int maxExcl)
        {
            if (maxExcl <= minIncl)
                throw new ArgumentOutOfRangeException(nameof(maxExcl), $"maxExcl ({maxExcl}) must be greater than minIncl ({minIncl}).");
            unchecked
            {
                uint bound = (uint)((long)maxExcl - minIncl);
                // Unbiased: reject values below 2^32 mod bound.
                uint threshold = (uint)(-bound) % bound;
                while (true)
                {
                    uint r = NextUInt();
                    if (r >= threshold) return (int)(minIncl + (long)(r % bound));
                }
            }
        }

        public float Range(float min, float max)
        {
            if (max < min)
                throw new ArgumentOutOfRangeException(nameof(max), $"max ({max}) must not be less than min ({min}).");
            return min + (max - min) * Value01();
        }

        public IRng Derive(string name, int index = 0) => new Pcg32Rng(RngHash.ChildSeed(_seed, name, index));
    }
}
