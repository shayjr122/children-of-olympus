using GMR.Core.Abilities;
using GMR.Core.Combat;
using GMR.Core.Combo;
using GMR.Core.Encounter;
using GMR.Core.Feedback;
using GMR.Core.Rage;
using GMR.Core.Rng;
using GMR.Core.Status;
using GMR.Runtime.Encounter;
using UnityEngine;

namespace GMR.Runtime.Feedback
{
    /// <summary>
    /// Maps combat events to placeholder feedback through FeedbackConfig (FR-27, AD-11): target flash, hitstop via
    /// TimeScaleService, floating damage numbers, camera shake on heavy hits, Hero red flash on PlayerDamaged, an
    /// "AVOIDED" popup on HitAvoided, Ultimate hitstop/shake, a "NOT READY" popup when the Ultimate is pressed below full
    /// RAGE, a "CONDUCTED!" popup on Conducted Shock (FR-21) and SFX hooks (hit, DASH, Perfect Dodge, Ultimate, RAGE full,
    /// Combo lost, Conducted, ...). Subscribes to CombatEvents only;
    /// gameplay never calls feedback. Toggles: FeedbackConfig default or pause-menu override.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class FeedbackRouter : MonoBehaviour
    {
        [SerializeField] EncounterBootstrap _bootstrap;
        [SerializeField] FeedbackConfig _config;
        [SerializeField] PopupSpawner _popups;
        [SerializeField] CameraShake _cameraShake;
        [SerializeField] SfxHookPlayer _sfx;
        [SerializeField] Color _avoidedColor = new Color(0.5f, 0.9f, 1f, 1f);

        CombatEvents _events;
        int _conductedPopupFrame = -1;
        int _conductedCueFrame = -1, _chainCueFrame = -1, _statusCueFrame = -1;

        EncounterContext Context => _bootstrap.Context;
        bool DamageNumbersOn => SessionToggles.Resolve(Context.Toggles.DamageNumbers, _config.DamageNumbers);
        bool HitstopOn => SessionToggles.Resolve(Context.Toggles.Hitstop, _config.Hitstop);
        bool ShakeOn => SessionToggles.Resolve(Context.Toggles.ScreenShake, _config.ScreenShake);

        void OnEnable()
        {
            if (_bootstrap == null || _config == null)
            {
                UnityEngine.Debug.LogError("[GMR/Feedback] FeedbackRouter needs a bootstrap and a FeedbackConfig.", this);
                return;
            }
            _events = Context.Events;
            _events.OnHitLanded += OnHitLanded;
            _events.OnHitAvoided += OnHitAvoided;
            _events.OnPlayerDamaged += OnPlayerDamaged;
            _events.OnEnemyDied += OnEnemyDied;
            _events.OnDashStarted += OnDashStarted;
            _events.OnPerfectDodged += OnPerfectDodged;
            _events.OnAbilityUsed += OnAbilityUsed;
            _events.OnUltimateUsed += OnUltimateUsed;
            _events.OnUltimateNotReady += OnUltimateNotReady;
            _events.OnRageFull += OnRageFull;
            _events.OnComboReset += OnComboReset;
            _events.OnConductedShock += OnConductedShock;
            _events.OnChainJumped += OnChainJumped;
            _events.OnStatusApplied += OnStatusApplied;
        }

        void OnDisable()
        {
            if (_events == null) return;
            _events.OnHitLanded -= OnHitLanded;
            _events.OnHitAvoided -= OnHitAvoided;
            _events.OnPlayerDamaged -= OnPlayerDamaged;
            _events.OnEnemyDied -= OnEnemyDied;
            _events.OnDashStarted -= OnDashStarted;
            _events.OnPerfectDodged -= OnPerfectDodged;
            _events.OnAbilityUsed -= OnAbilityUsed;
            _events.OnUltimateUsed -= OnUltimateUsed;
            _events.OnUltimateNotReady -= OnUltimateNotReady;
            _events.OnRageFull -= OnRageFull;
            _events.OnComboReset -= OnComboReset;
            _events.OnConductedShock -= OnConductedShock;
            _events.OnChainJumped -= OnChainJumped;
            _events.OnStatusApplied -= OnStatusApplied;
            _events = null;
        }

        void OnHitLanded(HitLanded e)
        {
            bool heavy = e.Result.Weight == HitWeight.Heavy;
            bool ultimate = (e.Result.Tags & DamageTags.Ultimate) != 0;
            if (_config.HitFlash) FlashTarget(e.Target, _config.HitFlashColor, _config.HitFlashDuration);
            HitstopRule.TryRequest(Context.TimeScale, _config, HitstopOn, e.Result.Weight, e.Result.Tags,
                Context.TimeScale.UnscaledNow);
            ShowNumber(e.Result.Dealt, e.Position, heavy || ultimate ? _config.HeavyDamageNumberColor : _config.DamageNumberColor);
            if (ultimate) ShakeRaw(_config.UltimateShakeAmplitude, _config.UltimateShakeDuration);
            else if (heavy) Shake(1f);
            Sfx(heavy ? SfxCue.HeavyHit : SfxCue.Hit, e.Position);
        }

        void OnPlayerDamaged(PlayerDamaged e)
        {
            if (_config.HitFlash) FlashTarget(e.Target, _config.PlayerHurtColor, _config.PlayerHurtFlashDuration);
            ShowNumber(e.Result.Dealt, e.Position, _config.PlayerDamageNumberColor);
            Shake(_config.PlayerHurtShakeScale);
            Sfx(SfxCue.PlayerHurt, e.Position);
        }

        void OnHitAvoided(HitAvoided e)
        {
            if (_popups != null) _popups.Show("AVOIDED", e.Position, _avoidedColor);
            Sfx(SfxCue.Avoided, e.Position);
        }

        void OnEnemyDied(EnemyDied e) => Sfx(SfxCue.EnemyDied, e.Position);
        void OnDashStarted(DashStarted e) => Sfx(SfxCue.Dash, e.Position);
        void OnPerfectDodged(PerfectDodged e) => Sfx(SfxCue.PerfectDodge, e.Position);
        void OnAbilityUsed(AbilityUsed e) => Sfx(SfxCue.AbilityUsed, e.Position);
        void OnUltimateUsed(UltimateUsed e) => Sfx(SfxCue.UltimateUsed, e.Position);
        void OnRageFull(RageFull e) => Sfx(SfxCue.RageFull, PlayerPosition(e.Player));

        void OnComboReset(ComboReset e)
        {
            if (e.Reason == ComboResetReason.Damage) Sfx(SfxCue.ComboLost, PlayerPosition(e.Player));
        }

        void OnConductedShock(ConductedShock e)
        {
            // One popup per frame: a conducted chain resolves within one frame and would stack a popup on every target.
            if (_popups != null && _config.ConductedPopup && _conductedPopupFrame != Time.frameCount)
            {
                _conductedPopupFrame = Time.frameCount;
                _popups.Show("CONDUCTED!", e.Position + new Vector2(0f, 0.5f), _config.ConductedPopupColor);
            }
            if (_conductedCueFrame != Time.frameCount) { _conductedCueFrame = Time.frameCount; Sfx(SfxCue.Conducted, e.Position); }
        }

        // Status cues fire at most once per frame each: a chain resolves many applications in one frame.
        void OnChainJumped(ChainJumped e)
        {
            if (_chainCueFrame == Time.frameCount) return;
            _chainCueFrame = Time.frameCount;
            Sfx(SfxCue.ChainJump, e.ToPosition);
        }

        void OnStatusApplied(StatusApplied e)
        {
            if (!e.IsNew || _statusCueFrame == Time.frameCount) return;
            _statusCueFrame = Time.frameCount;
            Sfx(SfxCue.StatusApplied, e.Position);
        }

        void OnUltimateNotReady(UltimateNotReady e)
        {
            if (_popups != null) _popups.Show("NOT READY", e.Position, _config.UltimateNotReadyColor);
            Sfx(SfxCue.UltimateNotReady, e.Position);
        }

        Vector2 PlayerPosition(GMR.Core.Players.PlayerId id) =>
            Context.Players.TryGet(id, out var player) ? player.Position : Vector2.zero;

        void ShowNumber(float amount, Vector2 position, Color color)
        {
            if (_popups == null || !DamageNumbersOn || amount <= 0f) return;
            _popups.Show(Mathf.CeilToInt(amount).ToString(), position, color);
        }

        void Shake(float scale)
        {
            if (scale <= 0f) return;
            ShakeRaw(_config.ShakeAmplitude * scale, _config.ShakeDuration);
        }

        void ShakeRaw(float amplitude, float duration)
        {
            if (_cameraShake == null || !ShakeOn || amplitude <= 0f) return;
            _cameraShake.Shake(amplitude, duration, Context.Rng.Stream(RngStreams.Feedback));
        }

        void Sfx(SfxCue cue, Vector2 position)
        {
            if (_sfx != null) _sfx.Fire(cue, position);
        }

        static void FlashTarget(IDamageable target, Color color, float duration)
        {
            if (target is Component component && component != null &&
                component.TryGetComponent(out HitFlash flash))
                flash.Flash(color, duration);
        }
    }
}
