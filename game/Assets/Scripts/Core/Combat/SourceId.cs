using System;
using GMR.Core.Enemies;
using GMR.Core.Players;

namespace GMR.Core.Combat
{
    /// <summary>
    /// Tagged union of <see cref="PlayerId"/> and <see cref="EnemyId"/> (AD-4). Every damage request, status and
    /// combat event carries one, so per-Player systems (Combo, RAGE, telemetry) can filter by owner.
    /// </summary>
    public readonly struct SourceId : IEquatable<SourceId>
    {
        public readonly SourceKind Kind;
        public readonly int Value;

        SourceId(SourceKind kind, int value)
        {
            Kind = kind;
            Value = value;
        }

        public static SourceId ForPlayer(PlayerId player) => new SourceId(SourceKind.Player, player.Value);
        public static SourceId ForEnemy(EnemyId enemy) => new SourceId(SourceKind.Enemy, enemy.Value);

        public bool IsPlayer => Kind == SourceKind.Player;
        public bool IsEnemy => Kind == SourceKind.Enemy;

        /// <summary>The PlayerId; throws if this is not a Player source.</summary>
        public PlayerId Player => IsPlayer ? new PlayerId(Value) : throw new InvalidOperationException($"{this} is not a Player.");

        /// <summary>True if this source is <paramref name="player"/>.</summary>
        public bool Is(PlayerId player) => IsPlayer && Value == player.Value;

        public bool Equals(SourceId other) => Kind == other.Kind && Value == other.Value;
        public override bool Equals(object obj) => obj is SourceId other && Equals(other);
        public override int GetHashCode() => ((int)Kind * 397) ^ Value;

        public override string ToString() => Kind switch
        {
            SourceKind.Player => "P" + Value,
            SourceKind.Enemy => "E" + Value,
            _ => "none",
        };

        public static bool operator ==(SourceId a, SourceId b) => a.Equals(b);
        public static bool operator !=(SourceId a, SourceId b) => !a.Equals(b);
    }
}
