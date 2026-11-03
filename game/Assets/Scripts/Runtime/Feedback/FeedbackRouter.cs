using GMR.Core.Abilities;
using GMR.Core.Combat;
using GMR.Core.Encounter;
using GMR.Core.Feedback;
using GMR.Core.Rng;
using GMR.Runtime.Encounter;
using UnityEngine;

namespace GMR.Runtime.Feedback
{
    /// <summary>
    /// Maps combat events to placeholder feedback through FeedbackConfig (FR-27, AD-11): target flash, hitstop via
    /// TimeScaleService, floating damage numbers, camera shake on heavy hits, Hero red flash on PlayerDamaged, an
    /// "AVOIDED" popup on HitAvoided and SFX hooks (hit, DASH, Perfect Dodge, ...). Subscribes to CombatEvents only;
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
            _events = null;
        }

        void OnHitLanded(HitLanded e)
        {
            bool heavy = e.Result.Weight == HitWeight.Heavy;
            if (_config.HitFlash) FlashTarget(e.Target, _config.HitFlashColor, _config.HitFlashDuration);
            HitstopRule.TryRequest(Context.TimeScale, _config, HitstopOn, e.Result.Weight, Time.unscaledTime);
            ShowNumber(e.Result.Dealt, e.Position, heavy ? _config.HeavyDamageNumberColor : _config.DamageNumberColor);
            if (heavy) Shake(1f);
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

        void ShowNumber(float amount, Vector2 position, Color color)
        {
            if (_popups == null || !DamageNumbersOn || amount <= 0f) return;
            _popups.Show(Mathf.CeilToInt(amount).ToString(), position, color);
        }

        void Shake(float scale)
        {
            if (_cameraShake == null || !ShakeOn || scale <= 0f) return;
            _cameraShake.Shake(_config.ShakeAmplitude * scale, _config.ShakeDuration, Context.Rng.Stream(RngStreams.Feedback));
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
