using GMR.Core.Combat;
using UnityEngine;

namespace GMR.Core.Status
{
    /// <summary>
    /// Shock's chain lightning (FR-19, AD-10). Applying the status starts a <see cref="ChainRun"/> from the target: up to
    /// <see cref="MaxJumps"/> jumps (or <see cref="MaxChainJumps"/> for a max-potency application, Wrath of Zeus) to the
    /// nearest living enemy within <see cref="Radius"/>, each enemy at most once per chain, jump k dealing
    /// <see cref="Damage"/> x <see cref="Falloff"/>^k tagged Shock|ChainJump and attributed to the original source. Chain
    /// modifiers of the target a jump leaves (Wet, via the fold) add jumps/radius; Shock code knows nothing about Wet.
    /// </summary>
    [CreateAssetMenu(menuName = "GMR/Status/Behaviour/Chain Lightning", fileName = "Behaviour_ChainLightning_")]
    public sealed class ChainLightningBehaviour : StatusBehaviour
    {
        [SerializeField, Min(0), Tooltip("N: jumps after the first target.")]
        int _maxJumps = 3;

        [SerializeField, Min(0), Tooltip("Jumps for a max-potency application (Ultimate: Shock at maximum chain count).")]
        int _maxChainJumps = 6;

        [SerializeField, Min(1), Tooltip("Hard cap on jumps per chain, bonus jumps (Wet: +1 per hop) included.")]
        int _maxTotalJumps = 12;

        [SerializeField, Min(0f), Tooltip("R: search radius around the target a jump leaves, units.")]
        float _radius = 3.5f;

        [SerializeField, Range(0f, 1f), Tooltip("Damage factor per jump (jump k deals damage x falloff^k).")]
        float _falloff = 0.7f;

        [SerializeField, Min(0f), Tooltip("Shock damage before falloff.")]
        float _damage = 8f;

        [SerializeField, Tooltip("Tags of jump damage (ChainJump is always added).")]
        DamageTags _jumpTags = DamageTags.Shock;

        [SerializeField, Min(0f), Tooltip("Candidates whose distances differ by less than this tie; the Status RNG stream picks.")]
        float _tieEpsilon = 0.01f;

        public int MaxJumps { get => _maxJumps; internal set => _maxJumps = value; }
        public int MaxChainJumps { get => _maxChainJumps; internal set => _maxChainJumps = value; }
        public int MaxTotalJumps { get => Mathf.Max(1, _maxTotalJumps); internal set => _maxTotalJumps = value; }
        public float Radius { get => _radius; internal set => _radius = value; }
        public float Falloff { get => _falloff; internal set => _falloff = value; }
        public float Damage { get => _damage; internal set => _damage = value; }
        public DamageTags JumpTags { get => _jumpTags | DamageTags.ChainJump; internal set => _jumpTags = value; }
        public float TieEpsilon { get => _tieEpsilon; internal set => _tieEpsilon = value; }

        public override void OnApply(in StatusApplyContext c)
        {
            var info = c.Info;
            if (info.Chain != null)
            {
                // A jump of a running chain landed: continue that chain (with this target's chain bonus), never start another.
                if (info.Chain.Behaviour == this) info.Chain.Arrived(c.Target, info, c.Now);
                return;
            }
            if ((info.HitTags & DamageTags.ChainJump) != 0) return; // another chain's jump: no nested chains

            // An Ultimate's chain keeps the Ultimate tag, so its jumps never refill RAGE (RageSource.UltimateHit).
            var inherited = info.HitTags & DamageTags.Ultimate;
            var run = new ChainRun(this, c.Services, c.Definition, c.Source, info.MaxPotency ? _maxChainJumps : _maxJumps,
                inherited, c.Target);
            run.Arrived(c.Target, info, c.Now);
        }
    }
}
