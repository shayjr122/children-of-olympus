using GMR.Core.Controls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GMR.Runtime.Controls
{
    /// <summary>
    /// The only component that touches InputAction (AD-7). Each Player gets its own actions and its own InputFrame.
    /// When a PlayerInput sits on the same GameObject (PlayerInputManager join, AD-4) its per-Player, device-paired
    /// actions are used; otherwise GameControls is cloned. Presses are also pushed into the Player's InputBuffer.
    /// Runs before gameplay (-100) so gameplay reads this frame's input in Update.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        const string GameplayMap = "Gameplay";
        const string DebugMap = "Debug";

        [SerializeField, Tooltip("GameControls.inputactions. Cloned per Player at Awake when no PlayerInput is present.")]
        InputActionAsset _controls;

        InputActionAsset _instance;
        InputActionMap _gameplay;
        InputActionMap _debugMap;
        InputAction _move, _aim, _dash, _basic, _special, _signature, _ultimate, _directionLock, _heroSwap, _pause, _debugMenu;

        InputBuffer _buffer;
        InputFrame _pending;
        InputFrame _frame;

        /// <summary>This frame's input for this Player.</summary>
        public InputFrame Frame => _frame;

        int _discardFrame = -1;

        /// <summary>
        /// Presses read during the current frame are not buffered (used when the pause menu closes: the button that
        /// clicked Resume must not also DASH, whichever runs first, the EventSystem or this reader).
        /// </summary>
        public void DiscardPressesThisFrame() => _discardFrame = Time.frameCount;

        /// <summary>Connects this Player's InputBuffer; presses are pushed into it from now on (AD-7).</summary>
        public void SetBuffer(InputBuffer buffer) => _buffer = buffer;

        void Awake()
        {
            InputActionAsset actions;
            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null && playerInput.actions != null)
            {
                actions = playerInput.actions; // per-Player copy owned by PlayerInput
            }
            else if (_controls != null)
            {
                _instance = Instantiate(_controls);
                actions = _instance;
            }
            else
            {
                UnityEngine.Debug.LogError("[GMR/Controls] PlayerInputReader has no GameControls asset assigned.", this);
                enabled = false;
                return;
            }

            _gameplay = actions.FindActionMap(GameplayMap, throwIfNotFound: true);
            _move = _gameplay.FindAction("Move", true);
            _aim = _gameplay.FindAction("Aim", true);
            _dash = _gameplay.FindAction("Dash", true);
            _basic = _gameplay.FindAction("Basic", true);
            _special = _gameplay.FindAction("Special", true);
            _signature = _gameplay.FindAction("Signature", true);
            _ultimate = _gameplay.FindAction("Ultimate", true);
            _directionLock = _gameplay.FindAction("DirectionLock", true);
            _heroSwap = _gameplay.FindAction("HeroSwap", true);
            _pause = _gameplay.FindAction("Pause", true);

            _debugMap = actions.FindActionMap(DebugMap, throwIfNotFound: false);
            _debugMenu = _debugMap?.FindAction("ToggleDebugMenu", false);
        }

        void OnEnable()
        {
            if (_gameplay == null) return;
            _dash.performed += OnDash;
            _basic.performed += OnBasic;
            _special.performed += OnSpecial;
            _signature.performed += OnSignature;
            _ultimate.performed += OnUltimate;
            _heroSwap.performed += OnHeroSwap;
            _pause.performed += OnPause;
            if (_debugMenu != null) _debugMenu.performed += OnDebugMenu;
            _gameplay.Enable();
            _debugMap?.Enable();
        }

        void OnDisable()
        {
            if (_gameplay == null) return;
            _gameplay.Disable();
            _debugMap?.Disable();
            _dash.performed -= OnDash;
            _basic.performed -= OnBasic;
            _special.performed -= OnSpecial;
            _signature.performed -= OnSignature;
            _ultimate.performed -= OnUltimate;
            _heroSwap.performed -= OnHeroSwap;
            _pause.performed -= OnPause;
            if (_debugMenu != null) _debugMenu.performed -= OnDebugMenu;
            _pending.ClearPresses();
            _frame = default;
        }

        void OnDestroy()
        {
            if (_instance != null) Destroy(_instance);
        }

        void Update()
        {
            InputFrame next = _pending;
            next.Move = Vector2.ClampMagnitude(_move.ReadValue<Vector2>(), 1f);
            next.Aim = _aim.ReadValue<Vector2>();
            next.BasicHeld = _basic.IsPressed();
            next.DirectionLockHeld = _directionLock.IsPressed();
            _frame = next;
            _pending.ClearPresses();
            PushToBuffer(next);
        }

        void PushToBuffer(in InputFrame f)
        {
            // While paused (menu open) presses are UI presses (Submit = Dash button); never buffer them for gameplay.
            if (_buffer == null || Time.timeScale <= 0f || Time.frameCount == _discardFrame) return;
            float now = Time.time;
            if (f.DashPressed) _buffer.Press(BufferedAction.Dash, now);
            if (f.BasicPressed) _buffer.Press(BufferedAction.Basic, now);
            if (f.SpecialPressed) _buffer.Press(BufferedAction.Special, now);
            if (f.SignaturePressed) _buffer.Press(BufferedAction.Signature, now);
            if (f.UltimatePressed) _buffer.Press(BufferedAction.Ultimate, now);
            if (f.HeroSwapPressed) _buffer.Press(BufferedAction.HeroSwap, now);
        }

        void OnDash(InputAction.CallbackContext _) => _pending.DashPressed = true;
        void OnBasic(InputAction.CallbackContext _) => _pending.BasicPressed = true;
        void OnSpecial(InputAction.CallbackContext _) => _pending.SpecialPressed = true;
        void OnSignature(InputAction.CallbackContext _) => _pending.SignaturePressed = true;
        void OnUltimate(InputAction.CallbackContext _) => _pending.UltimatePressed = true;
        void OnHeroSwap(InputAction.CallbackContext _) => _pending.HeroSwapPressed = true;
        void OnPause(InputAction.CallbackContext _) => _pending.PausePressed = true;
        void OnDebugMenu(InputAction.CallbackContext _) => _pending.DebugMenuPressed = true;
    }
}
