using UnityEngine;

namespace GMR.Core.Rage
{
    /// <summary>
    /// RAGE tuning per Hero (FR-15, FR-16, NFR-4), referenced by HeroDefinition. Amounts are fractions of the full meter
    /// (0.02 = 2%). Read live by RageMeter.
    /// </summary>
    [CreateAssetMenu(menuName = "GMR/Config/Rage Config", fileName = "Config_Rage_")]
    public sealed class RageConfig : ScriptableObject
    {
        [Header("Fill (fraction of the meter)")]
        [SerializeField, Range(0f, 1f), Tooltip("Per direct hit dealt.")] float _hitGain = 0.02f;

        [SerializeField, Range(0f, 1f), Tooltip("Per status tick or chain-jump hit (damage tagged StatusTick or ChainJump).")]
        float _statusTickGain = 0.005f;

        [SerializeField, Min(1), Tooltip("A Combo milestone every this many hits (10, 20, ...).")]
        int _comboMilestoneEvery = 10;

        [SerializeField, Range(0f, 1f), Tooltip("Bonus at each Combo milestone.")] float _comboMilestoneGain = 0.05f;
        [SerializeField, Range(0f, 1f), Tooltip("Per Perfect Dodge (FR-7).")] float _perfectDodgeGain = 0.10f;

        [SerializeField, Range(0f, 1f), Tooltip("Per hit of the Player's own Ultimate. 0 = an Ultimate never refills RAGE.")]
        float _ultimateHitGain;

        [Header("Decay (FR-16): also when full")]
        [SerializeField, Tooltip("Off = RAGE never decays (debug; the pause menu can override per session).")]
        bool _decayEnabled = true;

        [SerializeField, Min(0f), Tooltip("Seconds after the last gain before decay starts (scaled time).")]
        float _decayGrace = 1.5f;

        [SerializeField, Min(0f), Tooltip("Decay rate in meter fraction per second (0.08 = 8%/s).")]
        float _decayRate = 0.08f;

        public float HitGain { get => _hitGain; internal set => _hitGain = value; }
        public float StatusTickGain { get => _statusTickGain; internal set => _statusTickGain = value; }
        public int ComboMilestoneEvery { get => _comboMilestoneEvery; internal set => _comboMilestoneEvery = value; }
        public float ComboMilestoneGain { get => _comboMilestoneGain; internal set => _comboMilestoneGain = value; }
        public float PerfectDodgeGain { get => _perfectDodgeGain; internal set => _perfectDodgeGain = value; }
        public float UltimateHitGain { get => _ultimateHitGain; internal set => _ultimateHitGain = value; }
        public bool DecayEnabled { get => _decayEnabled; internal set => _decayEnabled = value; }
        public float DecayGrace { get => _decayGrace; internal set => _decayGrace = value; }
        public float DecayRate { get => _decayRate; internal set => _decayRate = value; }

        /// <summary>The configured gain for <paramref name="source"/> (Debug fills the whole meter).</summary>
        public float AmountFor(RageSource source) => source switch
        {
            RageSource.Hit => _hitGain,
            RageSource.StatusTick => _statusTickGain,
            RageSource.ComboMilestone => _comboMilestoneGain,
            RageSource.PerfectDodge => _perfectDodgeGain,
            RageSource.UltimateHit => _ultimateHitGain,
            RageSource.Debug => 1f,
            _ => 0f,
        };

        /// <summary>True if Combo <paramref name="count"/> is a milestone (10, 20, ...).</summary>
        public bool IsComboMilestone(int count) => count > 0 && _comboMilestoneEvery > 0 && count % _comboMilestoneEvery == 0;
    }
}
