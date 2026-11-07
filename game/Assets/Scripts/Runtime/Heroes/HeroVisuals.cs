using GMR.Core.Combat;
using GMR.Core.Heroes;
using UnityEngine;

namespace GMR.Runtime.Heroes
{
    /// <summary>
    /// Placeholder readability for DASH (NFR-6): the body turns pale and see-through during i-frames, darkens during
    /// Dash Recovery, and a small bar under the Hero drains over the recovery. Read-only view of HeroRuntime state.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(HeroRuntime))]
    public sealed class HeroVisuals : MonoBehaviour
    {
        [SerializeField, Tooltip("Bar under the Hero, scaled on X by remaining Dash Recovery.")]
        SpriteRenderer _recoveryBar;

        [SerializeField, Range(0f, 1f)] float _iFrameAlpha = 0.35f;
        [SerializeField] Color _iFrameTint = new Color(0.75f, 0.95f, 1f, 1f);
        [SerializeField, Range(0f, 1f), Tooltip("Body brightness during Dash Recovery.")] float _recoveryBrightness = 0.45f;
        [SerializeField] float _recoveryBarWidth = 1f;

        HeroRuntime _hero;

        void Awake() => _hero = GetComponent<HeroRuntime>();

        void LateUpdate()
        {
            if (!_hero.IsInitialized || _hero.BodyRenderer == null) return;
            float now = Time.time;
            Color baseColor = _hero.Definition.PlaceholderColor;
            Color color = baseColor;

            if (_hero.Invulnerability.IsInvulnerableFrom(InvulnerabilitySource.Dash, now))
            {
                color = Color.Lerp(baseColor, _iFrameTint, 0.6f);
                color.a = _iFrameAlpha;
            }
            else if (_hero.Dash.Phase == DashPhase.Recovery)
            {
                color = new Color(baseColor.r * _recoveryBrightness, baseColor.g * _recoveryBrightness,
                    baseColor.b * _recoveryBrightness, 1f);
            }
            _hero.BodyRenderer.color = color;

            if (_recoveryBar != null)
            {
                float remaining = _hero.Dash.RecoveryRemaining01(now);
                bool show = remaining > 0f;
                if (_recoveryBar.enabled != show) _recoveryBar.enabled = show;
                if (show)
                {
                    var s = _recoveryBar.transform.localScale;
                    s.x = _recoveryBarWidth * remaining;
                    _recoveryBar.transform.localScale = s;
                }
            }
        }
    }
}
