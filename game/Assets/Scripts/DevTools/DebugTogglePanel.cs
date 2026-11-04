using GMR.Core.Combo;
using GMR.Core.Controls;
using GMR.Core.Encounter;
using GMR.Core.Feedback;
using GMR.Core.Rage;
using GMR.Runtime.Encounter;
using UnityEngine;

namespace GMR.DevTools
{
    /// <summary>
    /// Debug items of the pause menu (Stories 2.5, 3.4 and 4.1/4.2, FR-29 partial): toggles for Aim Assist, Direction Lock,
    /// input buffering, Perfect Dodge flash and time-slow, damage numbers, hitstop, screen shake, shrinking Combo Window
    /// and RAGE decay, plus the "Fill RAGE" cheat (every joined Player). Each toggle writes a session override on
    /// EncounterContext.Toggles (Config assets stay read-only), applies immediately and persists until the scene reloads.
    /// </summary>
    public sealed class DebugTogglePanel : MonoBehaviour
    {
        [SerializeField] EncounterBootstrap _bootstrap;
        [SerializeField] AimConfig _aimConfig;
        [SerializeField] InputConfig _inputConfig;
        [SerializeField] FeedbackConfig _feedbackConfig;
        [SerializeField] ComboConfig _comboConfig;
        [SerializeField, Tooltip("Default shown for the RAGE decay toggle (the starting Hero's RageConfig).")] RageConfig _rageConfig;

        [SerializeField] UnityEngine.UI.Toggle _aimAssist;
        [SerializeField] UnityEngine.UI.Toggle _directionLock;
        [SerializeField] UnityEngine.UI.Toggle _inputBuffer;
        [SerializeField] UnityEngine.UI.Toggle _perfectDodgeFlash;
        [SerializeField] UnityEngine.UI.Toggle _perfectDodgeTimeSlow;
        [SerializeField] UnityEngine.UI.Toggle _damageNumbers;
        [SerializeField] UnityEngine.UI.Toggle _hitstop;
        [SerializeField] UnityEngine.UI.Toggle _screenShake;
        [SerializeField] UnityEngine.UI.Toggle _shrinkingCombo;
        [SerializeField] UnityEngine.UI.Toggle _rageDecay;
        [SerializeField] UnityEngine.UI.Button _fillRage;

        SessionToggles Toggles => _bootstrap.Context.Toggles;

        void OnEnable()
        {
            if (_bootstrap == null || _aimConfig == null || _inputConfig == null || _feedbackConfig == null ||
                _comboConfig == null || _rageConfig == null)
            {
                UnityEngine.Debug.LogError("[GMR/DevTools] DebugTogglePanel is missing references.", this);
                return;
            }
            var t = Toggles;
            Bind(_aimAssist, SessionToggles.Resolve(t.AimAssist, _aimConfig.AssistEnabled), OnAimAssist);
            Bind(_directionLock, SessionToggles.Resolve(t.DirectionLock, _aimConfig.DirectionLockEnabled), OnDirectionLock);
            Bind(_inputBuffer, SessionToggles.Resolve(t.InputBuffering, _inputConfig.BufferEnabled), OnInputBuffer);
            Bind(_perfectDodgeFlash, SessionToggles.Resolve(t.PerfectDodgeFlash, _feedbackConfig.PerfectDodgeFlash), OnPerfectDodgeFlash);
            Bind(_perfectDodgeTimeSlow, SessionToggles.Resolve(t.PerfectDodgeTimeSlow, _feedbackConfig.PerfectDodgeTimeSlow), OnPerfectDodgeTimeSlow);
            Bind(_damageNumbers, SessionToggles.Resolve(t.DamageNumbers, _feedbackConfig.DamageNumbers), OnDamageNumbers);
            Bind(_hitstop, SessionToggles.Resolve(t.Hitstop, _feedbackConfig.Hitstop), OnHitstop);
            Bind(_screenShake, SessionToggles.Resolve(t.ScreenShake, _feedbackConfig.ScreenShake), OnScreenShake);
            Bind(_shrinkingCombo, SessionToggles.Resolve(t.ShrinkingComboWindow, _comboConfig.Mode == ComboMode.Shrinking), OnShrinkingCombo);
            Bind(_rageDecay, SessionToggles.Resolve(t.RageDecay, _rageConfig.DecayEnabled), OnRageDecay);
            if (_fillRage != null) _fillRage.onClick.AddListener(FillRage);
        }

        void OnDisable()
        {
            Unbind(_aimAssist, OnAimAssist);
            Unbind(_directionLock, OnDirectionLock);
            Unbind(_inputBuffer, OnInputBuffer);
            Unbind(_perfectDodgeFlash, OnPerfectDodgeFlash);
            Unbind(_perfectDodgeTimeSlow, OnPerfectDodgeTimeSlow);
            Unbind(_damageNumbers, OnDamageNumbers);
            Unbind(_hitstop, OnHitstop);
            Unbind(_screenShake, OnScreenShake);
            Unbind(_shrinkingCombo, OnShrinkingCombo);
            Unbind(_rageDecay, OnRageDecay);
            if (_fillRage != null) _fillRage.onClick.RemoveListener(FillRage);
        }

        /// <summary>Cheat: fills every joined Player's RAGE to 100% (raises RageChanged / RageFull as usual).</summary>
        public void FillRage()
        {
            if (_bootstrap == null) return;
            var players = _bootstrap.PlayerRoots;
            for (int i = 0; i < players.Count; i++) players[i].CombatState?.Rage.Add(RageSource.Debug);
            UnityEngine.Debug.Log($"[GMR/DevTools] Fill RAGE ({players.Count} Player(s))");
        }

        static void Bind(UnityEngine.UI.Toggle toggle, bool value, UnityEngine.Events.UnityAction<bool> handler)
        {
            if (toggle == null) return;
            toggle.SetIsOnWithoutNotify(value);
            toggle.onValueChanged.AddListener(handler);
        }

        static void Unbind(UnityEngine.UI.Toggle toggle, UnityEngine.Events.UnityAction<bool> handler)
        {
            if (toggle != null) toggle.onValueChanged.RemoveListener(handler);
        }

        void OnAimAssist(bool on) { Toggles.AimAssist = on; Log("Aim Assist", on); }
        void OnDirectionLock(bool on) { Toggles.DirectionLock = on; Log("Direction Lock", on); }
        void OnInputBuffer(bool on) { Toggles.InputBuffering = on; Log("Input buffer", on); }
        void OnPerfectDodgeFlash(bool on) { Toggles.PerfectDodgeFlash = on; Log("Perfect Dodge flash", on); }
        void OnPerfectDodgeTimeSlow(bool on) { Toggles.PerfectDodgeTimeSlow = on; Log("Perfect Dodge time-slow", on); }
        void OnDamageNumbers(bool on) { Toggles.DamageNumbers = on; Log("Damage numbers", on); }
        void OnHitstop(bool on) { Toggles.Hitstop = on; Log("Hitstop", on); }
        void OnScreenShake(bool on) { Toggles.ScreenShake = on; Log("Screen shake", on); }
        void OnShrinkingCombo(bool on) { Toggles.ShrinkingComboWindow = on; Log("Shrinking Combo window", on); }
        void OnRageDecay(bool on) { Toggles.RageDecay = on; Log("RAGE decay", on); }

        static void Log(string name, bool on) => UnityEngine.Debug.Log($"[GMR/DevTools] {name}: {(on ? "ON" : "OFF")}");
    }
}
