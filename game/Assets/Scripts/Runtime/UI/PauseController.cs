using GMR.Runtime.Encounter;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GMR.Runtime.UI
{
    /// <summary>
    /// Pause (FR-29, AD-12): any Player's Pause (Start / Esc) or debug button (Select / F1) toggles the menu, and the
    /// UI Cancel action (B / Esc, via MenuCancelHandler on the menu items) closes it. Pausing
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
        int _openedFrame = -1;
        int _closedFrame = -1;

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

            // Keep a gamepad/keyboard focus while open (a mouse click on empty space clears the selection).
            if (IsOpen && _eventSystem != null && _firstSelected != null && _eventSystem.currentSelectedGameObject == null)
                _eventSystem.SetSelectedGameObject(_firstSelected.gameObject);
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else if (_closedFrame != Time.frameCount) Open(); // Esc is Pause AND Cancel: never close+reopen in one frame
        }

        /// <summary>
        /// UI Cancel (B / Esc) closes the menu. Ignored in the frame the menu opened, because Esc is also the Pause
        /// button that just opened it.
        /// </summary>
        public void CloseFromCancel()
        {
            if (!IsOpen || _openedFrame == Time.frameCount) return;
            Close();
        }

        public void Open()
        {
            if (IsOpen || _bootstrap == null) return;
            _pauseHandle = _bootstrap.Context.TimeScale.Hold(0f);
            _openedFrame = Time.frameCount;
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
            _closedFrame = Time.frameCount;
            if (_timeScaleDriver != null) _timeScaleDriver.ApplyNow();
            if (_menuRoot != null) _menuRoot.SetActive(false);
            if (_eventSystem != null) _eventSystem.SetSelectedGameObject(null);
        }

        void OnDisable() => Close();
    }
}
