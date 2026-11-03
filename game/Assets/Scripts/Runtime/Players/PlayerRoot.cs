using System.Collections.Generic;
using GMR.Core.Controls;
using GMR.Core.Encounter;
using GMR.Core.Heroes;
using GMR.Core.Players;
using GMR.Runtime.Controls;
using GMR.Runtime.Heroes;
using UnityEngine;

namespace GMR.Runtime.Players
{
    /// <summary>
    /// Everything that belongs to the person, not the Hero (AD-6): input reader, input buffer, aim and (later) Combo,
    /// RAGE and HUD. Owns one replaceable child HeroRuntime built from a HeroDefinition. Registered in
    /// EncounterContext.Players under its PlayerId (AD-4); one PlayerRoot per joined Player.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class PlayerRoot : MonoBehaviour, IPlayerTarget
    {
        [SerializeField] PlayerInputReader _reader;
        [SerializeField, Tooltip("Hero built when this Player joins.")] HeroDefinition _startingHero;
        [SerializeField, Tooltip("Placeholder Hero prefab; the HeroDefinition supplies tuning and color.")] HeroRuntime _heroPrefab;
        [SerializeField] InputConfig _inputConfig;
        [SerializeField] AimConfig _aimConfig;

        readonly List<Vector2> _candidates = new List<Vector2>(16);

        public PlayerId Id { get; private set; }
        public EncounterContext Context { get; private set; }
        public AimResolver Aim { get; private set; }
        public InputBuffer Buffer { get; private set; }
        public HeroRuntime Hero { get; private set; }
        public AimConfig AimConfig => _aimConfig;
        public bool IsInitialized => Context != null;

        /// <summary>This frame's input for this Player.</summary>
        public InputFrame Frame => _reader != null ? _reader.Frame : default;

        /// <summary>Aim Assist effective state (Config default or pause-menu override).</summary>
        public bool AimAssistOn => Context != null && SessionToggles.Resolve(Context.Toggles.AimAssist, _aimConfig.AssistEnabled);

        /// <summary>Direction Lock effective state (Config default or pause-menu override).</summary>
        public bool DirectionLockOn => Context != null && SessionToggles.Resolve(Context.Toggles.DirectionLock, _aimConfig.DirectionLockEnabled);

        // IPlayerTarget: delegates to the current Hero so a swap never changes the Player's identity.
        public bool IsAlive => Hero != null;
        public Vector2 Position => Hero != null ? Hero.Position : (Vector2)transform.position;
        public float Radius => Hero != null ? Hero.Radius : 0f;
        public bool IsInvulnerable(float now) => Hero != null && Hero.Invulnerability.IsInvulnerable(now);
        public GMR.Core.Combat.IDamageable Damageable => Hero != null ? Hero : null;

        /// <summary>Called once by EncounterBootstrap when this Player joins. Does not rely on Awake having run.</summary>
        public void Init(EncounterContext context, PlayerId id, Vector2 spawnPosition)
        {
            if (Context != null)
            {
                UnityEngine.Debug.LogError($"[GMR/Players] PlayerRoot {Id} initialized twice.", this);
                return;
            }
            if (_reader == null) _reader = GetComponent<PlayerInputReader>();
            if (_reader == null || _startingHero == null || _heroPrefab == null || _inputConfig == null || _aimConfig == null ||
                _startingHero.Move == null || _startingHero.Dash == null || _startingHero.Health == null)
            {
                UnityEngine.Debug.LogError("[GMR/Players] PlayerRoot is missing its reader, HeroDefinition (with Move/Dash/Health configs), Hero prefab or config.", this);
                return;
            }

            Context = context;
            Id = id;
            Aim = new AimResolver(Vector2.right);
            Buffer = new InputBuffer(_inputConfig);
            _reader.SetBuffer(Buffer);
            gameObject.name = $"PlayerRoot {id}";
            BuildHero(_startingHero, spawnPosition);
            context.Players.Register(this);
        }

        /// <summary>Destroys the current Hero (if any) and builds <paramref name="definition"/> at <paramref name="position"/> (AD-6).</summary>
        public void BuildHero(HeroDefinition definition, Vector2 position)
        {
            if (Hero != null) Destroy(Hero.gameObject);
            Hero = Instantiate(_heroPrefab, position, Quaternion.identity, transform);
            Hero.Init(this, definition);
        }

        /// <summary>Drops buffered presses and ignores this frame's presses (pause menu closing).</summary>
        public void DiscardPendingPresses()
        {
            Buffer?.Clear();
            if (_reader != null) _reader.DiscardPressesThisFrame();
        }

        /// <summary>The direction an Ability would fire along now (Aim Assist applied if on), for indicators and Epic 3.</summary>
        public Vector2 ResolveFireDirection()
        {
            if (Context == null) return Vector2.right;
            Context.Enemies.FillAlivePositions(_candidates);
            return Aim.ResolveFire(Position, _candidates, _aimConfig, AimAssistOn);
        }

        /// <summary>True and the target position if Aim Assist would snap right now.</summary>
        public bool TryGetAssistTarget(out Vector2 target)
        {
            target = default;
            if (Context == null || !AimAssistOn) return false;
            Context.Enemies.FillAlivePositions(_candidates);
            if (!AimResolver.TryFindAssistTarget(Position, Aim.Direction, _candidates, _aimConfig.AssistHalfAngle,
                    _aimConfig.AssistRange, out int index)) return false;
            target = _candidates[index];
            return true;
        }

        void Update()
        {
            if (Context == null) return;
            Buffer.Enabled = SessionToggles.Resolve(Context.Toggles.InputBuffering, _inputConfig.BufferEnabled);
            if (Time.timeScale <= 0f || Hero == null) return; // paused: menu navigation must not steer the aim

            var frame = Frame;
            Vector2 move = MoveMath.ApplyDeadZone(frame.Move, Hero.Definition.Move.DeadZone);
            Aim.Update(move, frame.DirectionLockHeld, DirectionLockOn);
        }

        void OnDestroy()
        {
            Context?.Players.Unregister(this);
        }
    }
}
