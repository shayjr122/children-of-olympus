using System;

namespace GMR.Core.Enemies
{
    /// <summary>Identifies one enemy (or training dummy) in an Encounter (AD-4). Allocated by EnemyRegistry.</summary>
    public readonly struct EnemyId : IEquatable<EnemyId>
    {
        public readonly int Value;

        public EnemyId(int value)
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), "EnemyId must be >= 0.");
            Value = value;
        }

        public bool Equals(EnemyId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is EnemyId other && Equals(other);
        public override int GetHashCode() => Value;
        public override string ToString() => "E" + Value;

        public static bool operator ==(EnemyId a, EnemyId b) => a.Value == b.Value;
        public static bool operator !=(EnemyId a, EnemyId b) => a.Value != b.Value;
    }
}
