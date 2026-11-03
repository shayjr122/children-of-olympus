using GMR.Core.Heroes;
using UnityEngine;

namespace GMR.Runtime.Heroes
{
    /// <summary>
    /// Moves the Hero's Rigidbody2D in FixedUpdate (FR-1): free analog movement from MoveConfig (read live every
    /// step), or the DASH velocity while dashing. Walls stop the body through normal collision.
    /// </summary>
    [RequireComponent(typeof(HeroRuntime))]
    public sealed class HeroMover : MonoBehaviour
    {
        HeroRuntime _hero;
        bool _wasDashing;

        void Awake() => _hero = GetComponent<HeroRuntime>();

        void FixedUpdate()
        {
            if (!_hero.IsInitialized || _hero.Owner == null) return;
            var move = _hero.Definition.Move;
            if (move == null) return;
            var body = _hero.Body;

            _hero.Dash.Tick(Time.time);
            if (_hero.Dash.IsDashing)
            {
                body.linearVelocity = _hero.Dash.Velocity;
                _wasDashing = true;
                return;
            }

            if (_wasDashing)
            {
                // Leave the DASH at run speed instead of sliding down from DASH speed.
                body.linearVelocity = Vector2.ClampMagnitude(body.linearVelocity, move.MaxSpeed);
                _wasDashing = false;
            }

            Vector2 target = MoveMath.TargetVelocity(_hero.Owner.Frame.Move, move);
            body.linearVelocity = MoveMath.Step(body.linearVelocity, target, move, Time.fixedDeltaTime);
        }
    }
}
