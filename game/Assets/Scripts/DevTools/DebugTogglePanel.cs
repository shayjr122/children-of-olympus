using GMR.Core.Controls;
using GMR.Core.Encounter;
using GMR.Core.Feedback;
using GMR.Runtime.Encounter;
using UnityEngine;

namespace GMR.DevTools
{
    /// <summary>
    /// Debug items of the pause menu (Story 2.5, FR-29 partial): toggles for Aim Assist, Direction Lock, input
    /// buffering, Perfect Dodge flash and time-slow. Each writes a session override on EncounterContext.Toggles
    /// (Config assets stay read-only), applies immediately and persists until the scene is reloaded.
    /// </summary>
    public sealed class DebugTogglePanel : MonoBehaviour
    {
        [SerializeField] EncounterBootstrap _bootstrap;
        [SerializeField] AimConfig _aimConfig;
        [SerializeField] InputConfig _inputConfig;
        [SerializeField] FeedbackConfig _feedbackConfig;

        [SerializeField] UnityEngine.UI.Toggle _aimAssist;
        [SerializeField] UnityEngine.UI.Toggle _directionLock;
        [SerializeField] UnityEngine.UI.Toggle _inputBuffer;
        [SerializeField] UnityEngine.UI.Toggle _perfectDodgeFlash;
        [SerializeField] UnityEngine.UI.Toggle _perfectDodgeTimeSlow;

        SessionToggles Toggles => _bootstrap.Context.Toggles;

        void OnEnable()
        {
            if (_bootstrap == null || _aimConfig == null || _inputConfig == null || _feedbackConfig == null)
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
        }

        void OnDisable()
        {
            Unbind(_aimAssist, OnAimAssist);
            Unbind(_directionLock, OnDirectionLock);
            Unbind(_inputBuffer, OnInputBuffer);
            Unbind(_perfectDodgeFlash, OnPerfectDodgeFlash);
            Unbind(_perfectDodgeTimeSlow, OnPerfectDodgeTimeSlow);
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

        static void Log(string name, bool on) => UnityEngine.Debug.Log($"[GMR/DevTools] {name}: {(on ? "ON" : "OFF")}");
    }
}
