using GMR.Core.Combat;
using UnityEngine;

namespace GMR.Core.Status
{
    /// <summary>A status was applied (new or refreshed) to a target (AD-11). Raised only by StatusContainer (AD-16).</summary>
    public readonly struct StatusApplied
    {
        public readonly IDamageable Target;
        public readonly SourceId TargetId;
        public readonly Vector2 Position;
        public readonly StatusDefinition Status;

        /// <summary>Who applied it (Combo/RAGE attribution, co-op).</summary>
        public readonly SourceId Source;

        public readonly int Stacks;
        public readonly bool IsNew;
        public readonly float At;

        public StatusApplied(IDamageable target, SourceId targetId, Vector2 position, StatusDefinition status, SourceId source,
            int stacks, bool isNew, float at)
        {
            Target = target;
            TargetId = targetId;
            Position = position;
            Status = status;
            Source = source;
            Stacks = stacks;
            IsNew = isNew;
            At = at;
        }
    }

    /// <summary>A status instance ended (expired, consumed by a rule or cleared). Raised only by StatusContainer.</summary>
    public readonly struct StatusExpired
    {
        public readonly IDamageable Target;
        public readonly SourceId TargetId;
        public readonly StatusDefinition Status;
        public readonly SourceId Source;
        public readonly StatusEndReason Reason;
        public readonly float At;

        public StatusExpired(IDamageable target, SourceId targetId, StatusDefinition status, SourceId source,
            StatusEndReason reason, float at)
        {
            Target = target;
            TargetId = targetId;
            Status = status;
            Source = source;
            Reason = reason;
            At = at;
        }
    }

    /// <summary>
    /// An interaction rule fired on a hit (FR-21: Wet conducts Shock). Raised only by StatusContainer during the damage
    /// fold, once per conducted application. Carries what Session Telemetry (Epic 9) counts: source Player, target, tag.
    /// </summary>
    public readonly struct ConductedShock
    {
        public readonly IDamageable Target;
        public readonly SourceId TargetId;
        public readonly Vector2 Position;

        /// <summary>The Player (or enemy) whose hit was conducted.</summary>
        public readonly SourceId Source;

        /// <summary>The rule's conductedEventTag (e.g. "wet_conducts_shock").</summary>
        public readonly string Tag;

        /// <summary>The status on the target that conducted (Wet).</summary>
        public readonly StatusDefinition Conductor;

        /// <summary>Damage multiplier applied by this rule (1 if the hit was not tag-matched).</summary>
        public readonly float Multiplier;

        public readonly DamageTags HitTags;
        public readonly int HitId;
        public readonly float At;

        public ConductedShock(IDamageable target, SourceId targetId, Vector2 position, SourceId source, string tag,
            StatusDefinition conductor, float multiplier, DamageTags hitTags, int hitId, float at)
        {
            Target = target;
            TargetId = targetId;
            Position = position;
            Source = source;
            Tag = tag;
            Conductor = conductor;
            Multiplier = multiplier;
            HitTags = hitTags;
            HitId = hitId;
            At = at;
        }

        public bool IsChainJump => (HitTags & DamageTags.ChainJump) != 0;
    }

    /// <summary>
    /// Lightning jumped from one target to the next and landed (FR-19). Raised only by the chain (ChainRun) when the jump's
    /// hit has resolved. Feedback draws the line; <see cref="Conducted"/> marks a jump boosted by an interaction rule.
    /// </summary>
    public readonly struct ChainJumped
    {
        public readonly SourceId Source;
        public readonly IDamageable From;
        public readonly IDamageable To;
        public readonly Vector2 FromPosition;
        public readonly Vector2 ToPosition;

        /// <summary>1 for the first jump of the chain.</summary>
        public readonly int JumpIndex;

        public readonly bool Conducted;
        public readonly float At;

        public ChainJumped(SourceId source, IDamageable from, IDamageable to, Vector2 fromPosition, Vector2 toPosition,
            int jumpIndex, bool conducted, float at)
        {
            Source = source;
            From = from;
            To = to;
            FromPosition = fromPosition;
            ToPosition = toPosition;
            JumpIndex = jumpIndex;
            Conducted = conducted;
            At = at;
        }
    }
}
