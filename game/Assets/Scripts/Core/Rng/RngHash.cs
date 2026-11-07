using System;

namespace GMR.Core.Rng
{
    /// <summary>
    /// Stable hashing used to derive stream seeds. Never use string.GetHashCode (not stable across runtimes).
    /// </summary>
    public static class RngHash
    {
        const ulong FnvOffset = 14695981039346656037UL;
        const ulong FnvPrime = 1099511628211UL;

        /// <summary>SplitMix64 finalizer: a bijective 64-bit mix.</summary>
        public static ulong SplitMix64(ulong x)
        {
            unchecked
            {
                x += 0x9E3779B97F4A7C15UL;
                x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
                x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
                return x ^ (x >> 31);
            }
        }

        /// <summary>FNV-1a 64-bit over the UTF-16 code units of <paramref name="s"/> (low byte, then high byte).</summary>
        public static ulong Fnv1a64(string s)
        {
            if (s == null) throw new ArgumentNullException(nameof(s));
            unchecked
            {
                ulong h = FnvOffset;
                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];
                    h ^= (byte)(c & 0xFF);
                    h *= FnvPrime;
                    h ^= (byte)(c >> 8);
                    h *= FnvPrime;
                }
                return h;
            }
        }

        /// <summary>Seed of a named stream: SplitMix64(parentSeed ^ Fnv1a64(name)).</summary>
        public static ulong StreamSeed(ulong parentSeed, string name) => SplitMix64(parentSeed ^ Fnv1a64(name));

        /// <summary>Seed of a derived child: the named seed further mixed with the index.</summary>
        public static ulong ChildSeed(ulong parentSeed, string name, int index) =>
            SplitMix64(StreamSeed(parentSeed, name) ^ SplitMix64(unchecked((ulong)(uint)index)));
    }
}
