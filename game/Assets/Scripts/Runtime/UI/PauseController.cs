using GMR.Runtime.Encounter;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GMR.Runtime.UI
{
    /// <summary>
    /// Pause (FR-29, AD-12): any Player's Pause (Start / Esc) or debug button (Select / F1) toggles the menu. Pausing
    /// is a scale-0 hold on TimeScaleService; the menu is uGUI driven by InputSystemUIInputModule, so it works with
    /// gamepad only and keyboard only. Debug items inside the menu live in the DevTools assembly; this class does not know them.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public sealed class PauseController : MonoBehaviour
    {
        const int NoHandle = -1;

        [SerializeField] EncounterBootstrap _bootstrap;
        [SerializeField] TimeScaleServiceDriver _timeScaleDriver;
        [SerializeField] EventSystem _eventSystem;
        [SerializeField, Tooltip("Menu root, inactive while playing.")] GameObject _menuRoot;
        [SerializeField, Tooltip("Selected when the menu opens (gamepad/keyboard focus).")] UnityEngine.UI.Selectable _firstSelected;

        int _pauseHandle = NoHandle;

        public bool IsOpen => _pauseHandle != NoHandle;

        void Start()
        {
            if (_menuRoot != null && !IsOpen) _menuRoot.SetActive(false);
        }

        void Update()
        {
            if (_bootstrap == null) return;
            var players = _bootstrap.PlayerRoots;
            for (int i = 0; i < players.Count; i++)
            {
                var frame = players[i].Frame;
                if (!frame.PausePressed && !frame.DebugMenuPressed) continue;
                Toggle();
                break;
            }
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void Open()
        {
            if (IsOpen || _bootstrap == null) return;
            _pauseHandle = _bootstrap.Context.TimeScale.Hold(0f);
            if (_timeScaleDriver != null) _timeScaleDriver.ApplyNow();
            if (_menuRoot != null) _menuRoot.SetActive(true);
            if (_eventSystem != null && _firstSelected != null)
            {
                _eventSystem.SetSelectedGameObject(null);
                _eventSystem.SetSelectedGameObject(_firstSelected.gameObject);
            }
        }

        /// <summary>Closes the menu and releases the pause. Wired to the Resume button.</summary>
        public void Close()
        {
            if (!IsOpen) return;
            _bootstrap.Context.TimeScale.Release(_pauseHandle);
            // A gameplay press made in the frame the menu opened was stamped at the frozen clock; drop it.
            var players = _bootstrap.PlayerRoots;
            for (int i = 0; i < players.Count; i++) players[i].DiscardPendingPresses();
            _pauseHandle = NoHandle;
            if (_timeScaleDriver != null) _timeScaleDriver.ApplyNow();
            if (_menuRoot != null) _menuRoot.SetActive(false);
            if (_eventSystem != null) _eventSystem.SetSelectedGameObject(null);
        }

        void OnDisable() => Close();
    }
}
