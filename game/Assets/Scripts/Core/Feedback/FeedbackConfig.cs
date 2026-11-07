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

        [Header("Ultimate (FR-17): hits tagged Ultimate")]
        [SerializeField, Min(0f), Tooltip("Hitstop for Ultimate hits, real seconds (0 = none; respects the Hitstop toggle).")]
        float _ultimateHitstop = 0.15f;

        [SerializeField, Min(0f), Tooltip("Camera shake amplitude when an Ultimate's hits land, units.")] float _ultimateShakeAmplitude = 0.4f;
        [SerializeField, Min(0f), Tooltip("Ultimate camera shake length, real seconds.")] float _ultimateShakeDuration = 0.5f;
        [SerializeField, Tooltip("Full-screen flash when an Ultimate's hits land.")] bool _ultimateFlash = true;
        [SerializeField] Color _ultimateFlashColor = new Color(0.75f, 0.9f, 1f, 0.55f);
        [SerializeField, Min(0f), Tooltip("Ultimate screen flash fade, real seconds.")] float _ultimateFlashDuration = 0.35f;
        [SerializeField] Color _ultimateBoltColor = new Color(0.7f, 0.9f, 1f, 1f);
        [SerializeField, Min(0f), Tooltip("Lightning bolt placeholder lifetime per struck enemy, real seconds.")] float _ultimateBoltDuration = 0.35f;
        [SerializeField, Tooltip("Ring shown around the Hero during the Ultimate's startup (charge-up).")] Color _ultimateChargeColor = new Color(1f, 0.9f, 0.4f, 0.8f);
        [SerializeField, Tooltip("'NOT READY' popup color when the Ultimate is pressed below full RAGE.")] Color _ultimateNotReadyColor = new Color(1f, 0.45f, 0.4f, 1f);

        [Header("Statuses (FR-18..FR-21): chain lightning lines, Conducted Shock")]
        [SerializeField, Tooltip("Lightning line of a Shock chain jump.")] Color _chainColor = new Color(1f, 0.93f, 0.45f, 1f);
        [SerializeField, Tooltip("Lightning line of a conducted jump (leaving or landing on a Wet target): blue-white.")]
        Color _conductedChainColor = new Color(0.72f, 0.92f, 1f, 1f);
        [SerializeField, Min(0f), Tooltip("Line width, units.")] float _chainWidth = 0.1f;
        [SerializeField, Min(0f), Tooltip("Conducted line width, units.")] float _conductedChainWidth = 0.2f;
        [SerializeField, Min(0f), Tooltip("Line lifetime, real seconds.")] float _chainDuration = 0.35f;
        [SerializeField, Min(1), Tooltip("Zigzag segments per jump.")] int _chainSegments = 5;
        [SerializeField, Min(0f), Tooltip("Zigzag sideways jitter, units (Feedback RNG stream).")] float _chainJitter = 0.25f;
        [SerializeField, Tooltip("'CONDUCTED!' popup color.")] Color _conductedPopupColor = new Color(0.55f, 0.85f, 1f, 1f);
        [SerializeField, Tooltip("Show the 'CONDUCTED!' popup on Conducted Shock.")] bool _conductedPopup = true;

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

        public float UltimateHitstop { get => _ultimateHitstop; internal set => _ultimateHitstop = value; }
        public float UltimateShakeAmplitude { get => _ultimateShakeAmplitude; internal set => _ultimateShakeAmplitude = value; }
        public float UltimateShakeDuration { get => _ultimateShakeDuration; internal set => _ultimateShakeDuration = value; }
        public bool UltimateFlash { get => _ultimateFlash; internal set => _ultimateFlash = value; }
        public Color UltimateFlashColor { get => _ultimateFlashColor; internal set => _ultimateFlashColor = value; }
        public float UltimateFlashDuration { get => _ultimateFlashDuration; internal set => _ultimateFlashDuration = value; }
        public Color UltimateBoltColor { get => _ultimateBoltColor; internal set => _ultimateBoltColor = value; }
        public float UltimateBoltDuration { get => _ultimateBoltDuration; internal set => _ultimateBoltDuration = value; }
        public Color UltimateChargeColor { get => _ultimateChargeColor; internal set => _ultimateChargeColor = value; }
        public Color UltimateNotReadyColor { get => _ultimateNotReadyColor; internal set => _ultimateNotReadyColor = value; }

        public Color ChainColor { get => _chainColor; internal set => _chainColor = value; }
        public Color ConductedChainColor { get => _conductedChainColor; internal set => _conductedChainColor = value; }
        public float ChainWidth { get => _chainWidth; internal set => _chainWidth = value; }
        public float ConductedChainWidth { get => _conductedChainWidth; internal set => _conductedChainWidth = value; }
        public float ChainDuration { get => _chainDuration; internal set => _chainDuration = value; }
        public int ChainSegments { get => Mathf.Max(1, _chainSegments); internal set => _chainSegments = value; }
        public float ChainJitter { get => _chainJitter; internal set => _chainJitter = value; }
        public Color ConductedPopupColor { get => _conductedPopupColor; internal set => _conductedPopupColor = value; }
        public bool ConductedPopup { get => _conductedPopup; internal set => _conductedPopup = value; }

        public bool SfxHooks { get => _sfxHooks; internal set => _sfxHooks = value; }
        public bool LogSfxCues { get => _logSfxCues; internal set => _logSfxCues = value; }

        /// <summary>Hitstop length for a hit of <paramref name="weight"/>, in real seconds (0 = none).</summary>
        public float HitstopDuration(HitWeight weight) =>
            Mathf.Max(0f, weight == HitWeight.Heavy ? _hitstopHeavy : _hitstopLight);

        /// <summary>Hitstop for a hit with <paramref name="tags"/>: the Ultimate hitstop for Ultimate hits, else by weight.</summary>
        public float HitstopDuration(HitWeight weight, DamageTags tags) =>
            (tags & DamageTags.Ultimate) != 0 ? Mathf.Max(0f, _ultimateHitstop) : HitstopDuration(weight);
    }
}
