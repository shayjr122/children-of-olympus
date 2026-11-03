using GMR.Core.Combat;
using UnityEngine;

namespace GMR.Core.Feedback
{
    /// <summary>
    /// Feedback tuning (FR-7, FR-27, AD-11): Perfect Dodge flash/slow, hit flash, hitstop, screen shake, damage
    /// numbers and SFX hooks. Every effect has an on/off default; the pause menu can override some per session.
    /// Read live by FeedbackRouter and the feedback components.
    /// </summary>
    [CreateAssetMenu(menuName = "GMR/Config/Feedback Config", fileName = "Config_Feedback")]
    public sealed class FeedbackConfig : ScriptableObject
    {
        [Header("Perfect Dodge flash")]
        [SerializeField] bool _perfectDodgeFlash = true;

        [SerializeField, Min(0f), Tooltip("Screen flash fade time in real seconds.")]
        float _flashDuration = 0.15f;

        [SerializeField, Range(0f, 1f)] float _flashMaxAlpha = 0.45f;

        [Header("Perfect Dodge time-slow")]
        [SerializeField] bool _perfectDodgeTimeSlow = true;

        [SerializeField, Range(0f, 1f), Tooltip("Time scale while slowed (0.3 = 30% speed).")]
        float _timeSlowScale = 0.3f;

        [SerializeField, Min(0f), Tooltip("Slow-mo length in real seconds.")]
        float _timeSlowDuration = 0.15f;

        [Header("Popups (PERFECT / AVOIDED / damage numbers)")]
        [SerializeField, Min(0.05f), Tooltip("Popup lifetime in real seconds.")]
        float _popupDuration = 0.7f;

        [SerializeField, Min(0f), Tooltip("How far a popup rises over its lifetime, in units.")]
        float _popupRise = 0.8f;

        [Header("Hitstop (FR-27): 0 s disables")]
        [SerializeField] bool _hitstop = true;

        [SerializeField, Min(0f), Tooltip("Hitstop for light hits, real seconds.")]
        float _hitstopLight = 0.03f;

        [SerializeField, Min(0f), Tooltip("Hitstop for heavy hits, real seconds.")]
        float _hitstopHeavy = 0.08f;

        [SerializeField, Range(0.01f, 1f), Tooltip("Time scale during hitstop. Kept above 0 so input is still buffered.")]
        float _hitstopScale = 0.05f;

        [Header("Hit flash")]
        [SerializeField] bool _hitFlash = true;
        [SerializeField] Color _hitFlashColor = Color.white;
        [SerializeField, Min(0f), Tooltip("Enemy flash length, real seconds.")] float _hitFlashDuration = 0.1f;
        [SerializeField] Color _playerHurtColor = new Color(1f, 0.15f, 0.1f, 1f);
        [SerializeField, Min(0f), Tooltip("Hero red flash length, real seconds.")] float _playerHurtFlashDuration = 0.25f;

        [Header("Damage numbers")]
        [SerializeField] bool _damageNumbers = true;
        [SerializeField] Color _damageNumberColor = new Color(1f, 0.95f, 0.8f, 1f);
        [SerializeField] Color _heavyDamageNumberColor = new Color(1f, 0.75f, 0.2f, 1f);
        [SerializeField] Color _playerDamageNumberColor = new Color(1f, 0.35f, 0.3f, 1f);

        [Header("Screen shake (heavy hits)")]
        [SerializeField] bool _screenShake = true;
        [SerializeField, Min(0f), Tooltip("Max camera offset in units.")] float _shakeAmplitude = 0.15f;
        [SerializeField, Min(0f), Tooltip("Shake length, real seconds.")] float _shakeDuration = 0.15f;
        [SerializeField, Min(0f), Tooltip("Shake amplitude multiplier when the Player is hit (0 = no shake).")] float _playerHurtShakeScale = 0.6f;

        [Header("SFX hooks (placeholder: silent unless clips are assigned on SfxHookPlayer)")]
        [SerializeField] bool _sfxHooks = true;
        [SerializeField, Tooltip("Log every fired SFX cue to the Console.")] bool _logSfxCues;

        public bool PerfectDodgeFlash { get => _perfectDodgeFlash; internal set => _perfectDodgeFlash = value; }
        public float FlashDuration { get => _flashDuration; internal set => _flashDuration = value; }
        public float FlashMaxAlpha { get => _flashMaxAlpha; internal set => _flashMaxAlpha = value; }
        public bool PerfectDodgeTimeSlow { get => _perfectDodgeTimeSlow; internal set => _perfectDodgeTimeSlow = value; }
        public float TimeSlowScale { get => _timeSlowScale; internal set => _timeSlowScale = value; }
        public float TimeSlowDuration { get => _timeSlowDuration; internal set => _timeSlowDuration = value; }
        public float PopupDuration { get => _popupDuration; internal set => _popupDuration = value; }
        public float PopupRise { get => _popupRise; internal set => _popupRise = value; }

        public bool Hitstop { get => _hitstop; internal set => _hitstop = value; }
        public float HitstopLight { get => _hitstopLight; internal set => _hitstopLight = value; }
        public float HitstopHeavy { get => _hitstopHeavy; internal set => _hitstopHeavy = value; }
        public float HitstopScale { get => _hitstopScale; internal set => _hitstopScale = value; }

        public bool HitFlash { get => _hitFlash; internal set => _hitFlash = value; }
        public Color HitFlashColor { get => _hitFlashColor; internal set => _hitFlashColor = value; }
        public float HitFlashDuration { get => _hitFlashDuration; internal set => _hitFlashDuration = value; }
        public Color PlayerHurtColor { get => _playerHurtColor; internal set => _playerHurtColor = value; }
        public float PlayerHurtFlashDuration { get => _playerHurtFlashDuration; internal set => _playerHurtFlashDuration = value; }

        public bool DamageNumbers { get => _damageNumbers; internal set => _damageNumbers = value; }
        public Color DamageNumberColor { get => _damageNumberColor; internal set => _damageNumberColor = value; }
        public Color HeavyDamageNumberColor { get => _heavyDamageNumberColor; internal set => _heavyDamageNumberColor = value; }
        public Color PlayerDamageNumberColor { get => _playerDamageNumberColor; internal set => _playerDamageNumberColor = value; }

        public bool ScreenShake { get => _screenShake; internal set => _screenShake = value; }
        public float ShakeAmplitude { get => _shakeAmplitude; internal set => _shakeAmplitude = value; }
        public float ShakeDuration { get => _shakeDuration; internal set => _shakeDuration = value; }
        public float PlayerHurtShakeScale { get => _playerHurtShakeScale; internal set => _playerHurtShakeScale = value; }

        public bool SfxHooks { get => _sfxHooks; internal set => _sfxHooks = value; }
        public bool LogSfxCues { get => _logSfxCues; internal set => _logSfxCues = value; }

        /// <summary>Hitstop length for a hit of <paramref name="weight"/>, in real seconds (0 = none).</summary>
        public float HitstopDuration(HitWeight weight) =>
            Mathf.Max(0f, weight == HitWeight.Heavy ? _hitstopHeavy : _hitstopLight);
    }
}
