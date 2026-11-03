using GMR.Core.Controls;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GMR.Runtime.Controls
{
    /// <summary>
    /// The only component that touches InputAction (AD-7). Each Player gets its own copy of GameControls and
    /// its own InputFrame. Runs before gameplay (-100) so gameplay reads this frame's input in Update.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        const string GameplayMap = "Gameplay";

        [SerializeField, Tooltip("GameControls.inputactions. Cloned per Player at Awake.")]
        InputActionAsset _controls;

        InputActionAsset _instance;
        InputActionMap _gameplay;
        InputAction _move, _aim, _dash, _basic, _special, _signature, _ultimate, _directionLock, _heroSwap, _pause;

        InputFrame _pending;
        InputFrame _frame;

        /// <summary>This frame's input for this Player.</summary>
        public InputFrame Frame => _frame;

        void Awake()
        {
            if (_controls == null)
            {
                UnityEngine.Debug.LogError("[GMR/Controls] PlayerInputReader has no GameControls asset assigned.", this);
                enabled = false;
                return;
            }

            _instance = Instantiate(_controls);
            _gameplay = _instance.FindActionMap(GameplayMap, throwIfNotFound: true);
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
            _gameplay.Enable();
        }

        void OnDisable()
        {
            if (_gameplay == null) return;
            _gameplay.Disable();
            _dash.performed -= OnDash;
            _basic.performed -= OnBasic;
            _special.performed -= OnSpecial;
            _signature.performed -= OnSignature;
            _ultimate.performed -= OnUltimate;
            _heroSwap.performed -= OnHeroSwap;
            _pause.performed -= OnPause;
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
        }

        void OnDash(InputAction.CallbackContext _) => _pending.DashPressed = true;
        void OnBasic(InputAction.CallbackContext _) => _pending.BasicPressed = true;
        void OnSpecial(InputAction.CallbackContext _) => _pending.SpecialPressed = true;
        void OnSignature(InputAction.CallbackContext _) => _pending.SignaturePressed = true;
        void OnUltimate(InputAction.CallbackContext _) => _pending.UltimatePressed = true;
        void OnHeroSwap(InputAction.CallbackContext _) => _pending.HeroSwapPressed = true;
        void OnPause(InputAction.CallbackContext _) => _pending.PausePressed = true;
    }
}
