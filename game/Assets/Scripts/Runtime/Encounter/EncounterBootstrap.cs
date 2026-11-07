using System;
using System.Collections.Generic;
using GMR.Core.Encounter;
using GMR.Core.Players;
using GMR.Core.Status;
using GMR.Runtime.Players;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GMR.Runtime.Encounter
{
    /// <summary>
    /// One per scene (AD-5). Creates the EncounterContext and hands it to every Player that joins through the
    /// PlayerInputManager (manual join, AD-4). Scene objects reach the context through a serialized reference to this
    /// component; nothing searches for it at runtime.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class EncounterBootstrap : MonoBehaviour
    {
        [SerializeField, Tooltip("Run Seed for this Encounter (AD-2).")]
        long _runSeed = 12345;

        [SerializeField] PlayerInputManager _playerInputManager;

        [SerializeField, Tooltip("Status interaction rules in play (Rule_WetConductsShock, ...).")]
        StatusRuleSet _statusRules;

        [SerializeField, Tooltip("Join Player 0 automatically when the scene starts.")]
        bool _joinFirstPlayerOnStart = true;

        [SerializeField, Tooltip("Spawn point per PlayerId; extra Players reuse the last one with an offset.")]
        Transform[] _spawnPoints = Array.Empty<Transform>();

        readonly List<PlayerRoot> _players = new List<PlayerRoot>(4);
        EncounterContext _context;

        /// <summary>The Encounter's context. Created on first access so Awake/OnEnable order never matters.</summary>
        public EncounterContext Context => _context ??= CreateContext();

        /// <summary>Joined Players in join order.</summary>
        public IReadOnlyList<PlayerRoot> PlayerRoots => _players;

        /// <summary>Raised after a joined Player has been initialized.</summary>
        public event Action<PlayerRoot> PlayerJoined;

        EncounterContext CreateContext()
        {
            var context = new EncounterContext(unchecked((ulong)_runSeed));
            context.StatusServices.Rules = _statusRules;
            return context;
        }

        void Awake()
        {
            _ = Context;
            if (_playerInputManager == null)
            {
                UnityEngine.Debug.LogError("[GMR/Encounter] EncounterBootstrap has no PlayerInputManager.", this);
                return;
            }
            _playerInputManager.joinBehavior = PlayerJoinBehavior.JoinPlayersManually;
            _playerInputManager.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
        }

        void OnEnable()
        {
            if (_playerInputManager == null) return;
            _playerInputManager.onPlayerJoined += OnPlayerJoined;
            _playerInputManager.onPlayerLeft += OnPlayerLeft;
        }

        void OnDisable()
        {
            if (_playerInputManager == null) return;
            _playerInputManager.onPlayerJoined -= OnPlayerJoined;
            _playerInputManager.onPlayerLeft -= OnPlayerLeft;
        }

        void Start()
        {
            if (_joinFirstPlayerOnStart && _players.Count == 0 && _playerInputManager != null)
            {
                // No device given: the single Player auto-switches between keyboard and gamepad.
                _playerInputManager.JoinPlayer(0);
            }
        }

        // First Update of the frame (-200): opens a new DamageSystem window for the per-frame request cap (AD-16).
        // Physics callbacks (projectile hits) run before Update, so they count toward the previous window.
        void Update() => Context.Damage.BeginFrame();

        void OnPlayerJoined(PlayerInput playerInput)
        {
            var root = playerInput.GetComponent<PlayerRoot>();
            if (root == null)
            {
                UnityEngine.Debug.LogError("[GMR/Encounter] Joined player prefab has no PlayerRoot.", playerInput);
                return;
            }
            int index = playerInput.playerIndex;
            root.Init(Context, new PlayerId(index), SpawnPosition(index));
            _players.Add(root);
            UnityEngine.Debug.Log($"[GMR/Encounter] Player {index} joined ({playerInput.currentControlScheme}).");
            PlayerJoined?.Invoke(root);
        }

        void OnPlayerLeft(PlayerInput playerInput)
        {
            var root = playerInput.GetComponent<PlayerRoot>();
            if (root == null) return;
            _players.Remove(root);
            Context.Players.Unregister(root);
        }

        Vector2 SpawnPosition(int index)
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0) return new Vector2(index * 1.5f, 0f);
            if (index < _spawnPoints.Length && _spawnPoints[index] != null) return _spawnPoints[index].position;
            var last = _spawnPoints[_spawnPoints.Length - 1];
            Vector2 basePos = last != null ? (Vector2)last.position : Vector2.zero;
            return basePos + new Vector2(1.5f * (index - _spawnPoints.Length + 1), 0f);
        }
    }
}
