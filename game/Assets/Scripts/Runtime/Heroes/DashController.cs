using GMR.Core.Combat;
using GMR.Core.Controls;
using GMR.Core.Heroes;
using UnityEngine;

namespace GMR.Runtime.Heroes
{
    /// <summary>
    /// Starts DASHes (FR-6, FR-7). Consumes the buffered Dash press when the Hero is Ready, dashes along the movement
    /// direction (Aim Direction if no input), registers the start-only i-frame window, raises DashStarted and checks
    /// pending hits for a Perfect Dodge. Sole raiser of DashStarted and PerfectDodged (AD-16). Uses scaled time,
    /// the same clock as PendingHit.LandTime.
    /// </summary>
    [RequireComponent(typeof(HeroRuntime))]
    public sealed class DashController : MonoBehaviour
    {
        [SerializeField, Tooltip("Layers the Hero passes through while dashing (enemies). Walls are never listed here.")]
        LayerMask _passThroughWhileDashing;

        HeroRuntime _hero;
        bool _excluding;

        void Awake() => _hero = GetComponent<HeroRuntime>();

        void Update()
        {
            if (!_hero.IsInitialized || _hero.Owner == null || !_hero.Owner.IsInitialized) return;
            float now = Time.time;
            var dash = _hero.Dash;
            dash.Tick(now);
            UpdatePassThrough(dash.IsDashing);

            if (Time.timeScale <= 0f) return;
            if (!dash.CanStart(now)) return; // Dashing or Dash Recovery: a buffered press waits (or expires)

            var owner = _hero.Owner;
            if (!owner.Buffer.TryConsume(BufferedAction.Dash, now)) return;

            var config = _hero.Definition.Dash;
            Vector2 move = MoveMath.ApplyDeadZone(owner.Frame.Move, _hero.Definition.Move.DeadZone);
            Vector2 direction = move.sqrMagnitude > 0f ? move.normalized : owner.Aim.Direction;
            if (!dash.TryStart(direction, now, config)) return;

            _hero.Invulnerability.Register(now, config.IFrameDuration, InvulnerabilitySource.Dash);
            UpdatePassThrough(true);

            var context = owner.Context;
            Vector2 position = _hero.Position;
            context.Events.RaiseDashStarted(new DashStarted(owner.Id, dash.Direction, position, now));

            if (context.PendingHits.TryClaimPerfectDodge(owner.Id, position, _hero.Radius, now, config.PerfectWindow, out var dodged))
            {
                context.Events.RaisePerfectDodged(new PerfectDodged(owner.Id, dodged.AttackId, position, now));
                UnityEngine.Debug.Log($"[GMR/Combat] PerfectDodged attack={dodged.AttackId} player={owner.Id} " +
                                      $"lead={(dodged.LandTime - now) * 1000f:0}ms");
            }
        }

        void UpdatePassThrough(bool dashing)
        {
            if (dashing == _excluding) return;
            _excluding = dashing;
            _hero.Body.excludeLayers = dashing ? _passThroughWhileDashing : (LayerMask)0;
        }

        void OnDisable()
        {
            if (_hero != null && _hero.Body != null) _hero.Body.excludeLayers = 0;
            _excluding = false;
        }
    }
}
