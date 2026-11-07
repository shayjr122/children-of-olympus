using System;

namespace GMR.Core.Players
{
    /// <summary>Identifies one Player (person) in an Encounter: 0..N-1 (AD-4). Never "the" player.</summary>
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        public readonly int Value;

        public PlayerId(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "PlayerId must be >= 0.");
            Value = value;
        }

        public bool Equals(PlayerId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => "P" + Value;

        public static bool operator ==(PlayerId a, PlayerId b) => a.Value == b.Value;
        public static bool operator !=(PlayerId a, PlayerId b) => a.Value != b.Value;
    }
}
