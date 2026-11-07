using GMR.Core.Abilities;
using GMR.Core.Combat;
using GMR.Core.Combo;
using GMR.Core.Encounter;
using GMR.Core.Rage;
using GMR.Runtime.Players;
using UnityEngine;

namespace GMR.Runtime.UI
{
    /// <summary>
    /// One Player's combat HUD panel (FR-28, FR-13 HUD part, AD-4): HP (number + bar), Combo count with a draining
    /// window bar and a reset cue, RAGE meter with a distinct full ("READY") state and a "not ready" blink, the three
    /// ability-slot cooldowns and a Run Seed field. Bound by CombatHud to exactly one PlayerRoot (no static lookup).
    /// Read-only view: polls its Player's state each frame (texts reassigned only when they change) and subscribes to
    /// CombatEvents for the one-shot cues, unsubscribing in OnDisable. Animates on unscaled time.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class PlayerHudPanel : MonoBehaviour
    {
        [Header("Header")]
        [SerializeField] TMPro.TMP_Text _title;
        [SerializeField] TMPro.TMP_Text _seed;

        [Header("HP")]
        [SerializeField, Tooltip("Left-anchored fill: anchorMax.x = HP fraction.")] RectTransform _hpFill;
        [SerializeField] UnityEngine.UI.Image _hpFillImage;
        [SerializeField] TMPro.TMP_Text _hpText;
        [SerializeField] Color _hpColor = new Color(0.35f, 0.9f, 0.35f, 1f);
        [SerializeField] Color _hpLowColor = new Color(1f, 0.3f, 0.25f, 1f);
        [SerializeField, Range(0f, 1f)] float _hpLowFraction = 0.3f;

        [Header("Combo")]
        [SerializeField] TMPro.TMP_Text _comboCount;
        [SerializeField, Tooltip("Left-anchored fill: anchorMax.x = window remaining.")] RectTransform _comboWindowFill;
        [SerializeField] UnityEngine.UI.Image _comboWindowImage;
        [SerializeField] TMPro.TMP_Text _comboResetCue;
        [SerializeField] Color _comboColor = new Color(1f, 0.85f, 0.35f, 1f);
        [SerializeField] Color _comboIdleColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] Color _comboLostColor = new Color(1f, 0.3f, 0.25f, 1f);
        [SerializeField] Color _comboDroppedColor = new Color(0.75f, 0.75f, 0.8f, 1f);
        [SerializeField, Min(0.05f), Tooltip("Reset cue lifetime, real seconds.")] float _resetCueDuration = 0.8f;

        [Header("RAGE")]
        [SerializeField, Tooltip("Left-anchored fill: anchorMax.x = RAGE.")] RectTransform _rageFill;
        [SerializeField] UnityEngine.UI.Image _rageFillImage;
        [SerializeField] UnityEngine.UI.Image _rageFrame;
        [SerializeField] TMPro.TMP_Text _rageLabel;
        [SerializeField, Tooltip("Ultimate button(s) shown in the READY label.")] string _ultimateHint = "RT / R";
        [SerializeField] Color _rageColor = new Color(0.85f, 0.25f, 0.15f, 1f);
        [SerializeField] Color _rageFullColor = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] Color _rageFrameColor = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] Color _rageNotReadyColor = new Color(1f, 0.2f, 0.2f, 1f);
        [SerializeField, Min(0.05f), Tooltip("'Not ready' blink length, real seconds.")] float _notReadyBlink = 0.35f;

        [Header("Ability slots (Basic, Special, Signature)")]
        [SerializeField] HudSlotView[] _slots = new HudSlotView[3];
        [SerializeField] Color _slotReadyColor = new Color(0.2f, 0.22f, 0.3f, 0.85f);
        [SerializeField] Color _slotBusyColor = new Color(0.45f, 0.4f, 0.2f, 0.9f);

        static readonly AbilitySlot[] SlotOrder = { AbilitySlot.Basic, AbilitySlot.Special, AbilitySlot.Signature };

        PlayerRoot _root;
        EncounterContext _context;
        CombatEvents _events;
        int _lastHp = int.MinValue, _lastMaxHp = int.MinValue, _lastCombo = int.MinValue, _lastRagePercent = int.MinValue;
        bool _lastFull;
        GMR.Core.Heroes.HeroDefinition _titleHero;
        float _cueRemaining;
        float _notReadyRemaining;

        public PlayerRoot Root => _root;

        /// <summary>The Combo number shown right now (for tests).</summary>
        public string ComboText => _comboCount != null ? _comboCount.text : string.Empty;

        /// <summary>The RAGE label shown right now (for tests).</summary>
        public string RageText => _rageLabel != null ? _rageLabel.text : string.Empty;

        /// <summary>Binds this panel to one Player. Called once by CombatHud right after instantiation.</summary>
        public void Bind(PlayerRoot root, EncounterContext context)
        {
            Unsubscribe();
            _root = root;
            _context = context;
            if (_seed != null && context != null) _seed.text = "SEED " + context.Rng.Seed;
            if (_comboResetCue != null) _comboResetCue.text = string.Empty;
            if (isActiveAndEnabled) Subscribe();
            Refresh();
        }

        void OnEnable() => Subscribe();
        void OnDisable() => Unsubscribe();

        void Subscribe()
        {
            if (_context == null || _events != null) return;
            _events = _context.Events;
            _events.OnComboReset += OnComboReset;
            _events.OnUltimateNotReady += OnUltimateNotReady;
        }

        void Unsubscribe()
        {
            if (_events == null) return;
            _events.OnComboReset -= OnComboReset;
            _events.OnUltimateNotReady -= OnUltimateNotReady;
            _events = null;
        }

        bool Mine(GMR.Core.Players.PlayerId id) => _root != null && _root.IsInitialized && id == _root.Id;

        void OnComboReset(ComboReset e)
        {
            if (!Mine(e.Player) || _comboResetCue == null) return;
            bool damage = e.Reason == ComboResetReason.Damage;
            _comboResetCue.text = (damage ? "LOST x" : "DROP x") + e.FinalCount;
            _comboResetCue.color = damage ? _comboLostColor : _comboDroppedColor;
            _cueRemaining = _resetCueDuration;
        }

        void OnUltimateNotReady(UltimateNotReady e)
        {
            if (Mine(e.Player)) _notReadyRemaining = _notReadyBlink;
        }

        void LateUpdate()
        {
            if (_root == null) return; // CombatHud destroys panels whose Player left
            Refresh();
            AnimateCues(GMR.Runtime.Encounter.FrameClock.UnscaledDeltaTime);
        }

        void Refresh()
        {
            if (_root == null || !_root.IsInitialized) return;
            var hero = _root.Hero;
            var state = _root.CombatState;

            var heroDefinition = hero != null ? hero.Definition : null;
            if (_title != null && (heroDefinition != _titleHero || _title.text.Length == 0))
            {
                _titleHero = heroDefinition; // rebuilt only when the Hero changes (no per-frame string garbage)
                _title.text = "P" + (_root.Id.Value + 1) + "  " + (heroDefinition != null ? heroDefinition.DisplayName.ToUpperInvariant() : "-");
            }

            if (hero != null && hero.Health != null)
            {
                float f = Mathf.Clamp01(hero.Health.Fraction);
                SetFill(_hpFill, f);
                if (_hpFillImage != null) _hpFillImage.color = f <= _hpLowFraction ? _hpLowColor : _hpColor;
                int hp = Mathf.CeilToInt(hero.Health.Current), max = Mathf.CeilToInt(hero.Health.Max);
                if (_hpText != null && (hp != _lastHp || max != _lastMaxHp))
                {
                    _lastHp = hp;
                    _lastMaxHp = max;
                    _hpText.text = hp + "/" + max;
                }
            }

            if (state != null)
            {
                var combo = state.Combo;
                if (_comboCount != null && combo.Count != _lastCombo)
                {
                    _lastCombo = combo.Count;
                    _comboCount.text = combo.Count > 0 ? "x" + combo.Count : "x0";
                    _comboCount.color = combo.Count > 0 ? _comboColor : _comboIdleColor;
                }
                SetFill(_comboWindowFill, combo.WindowRemaining01);

                var rage = state.Rage;
                SetFill(_rageFill, rage.Value01);
                int percent = Mathf.FloorToInt(rage.Value01 * 100f + 1e-3f);
                bool full = rage.IsFull;
                if (_rageLabel != null && (percent != _lastRagePercent || full != _lastFull))
                {
                    _lastRagePercent = percent;
                    _lastFull = full;
                    _rageLabel.text = full ? "RAGE READY!  " + _ultimateHint : "RAGE " + percent + "%";
                }
            }

            RefreshSlots(hero != null ? hero.Abilities : null);
        }

        void RefreshSlots(AbilityLoadout loadout)
        {
            if (_slots == null) return;
            float now = Time.time;
            for (int i = 0; i < _slots.Length && i < SlotOrder.Length; i++)
            {
                var view = _slots[i];
                if (view == null) continue;
                var runner = loadout?.Get(SlotOrder[i]);
                string name = runner != null ? view.Glyph + " " + ShortName(runner.Definition.DisplayName) : view.Glyph + " -";
                if (view.Name != null && name != view.LastName) { view.LastName = name; view.Name.text = name; }

                float remaining = runner != null ? runner.CooldownRemaining(now) : 0f;
                float cooldown = runner != null ? runner.EffectiveCooldown : 0f;
                int tenths = runner == null ? -2 : remaining > 0f ? Mathf.CeilToInt(remaining * 10f) : (runner.IsBusy ? -1 : 0);
                if (view.Value != null && tenths != view.LastTenths)
                {
                    view.LastTenths = tenths;
                    view.Value.text = tenths == -2 ? "" : tenths > 0 ? (tenths / 10f).ToString("0.0") : tenths == -1 ? "..." : "READY";
                }
                float overlay = cooldown > 0f ? Mathf.Clamp01(remaining / cooldown) : 0f;
                if (view.CooldownOverlay != null && !Mathf.Approximately(overlay, view.LastOverlay))
                {
                    view.LastOverlay = overlay;
                    SetFillVertical(view.CooldownOverlay, overlay);
                }
                if (view.Background != null) view.Background.color = runner != null && runner.IsBusy ? _slotBusyColor : _slotReadyColor;
            }
        }

        void AnimateCues(float dt)
        {
            if (_cueRemaining > 0f && _comboResetCue != null)
            {
                _cueRemaining -= dt;
                var c = _comboResetCue.color;
                c.a = Mathf.Clamp01(_cueRemaining / _resetCueDuration);
                _comboResetCue.color = c;
                if (_cueRemaining <= 0f) _comboResetCue.text = string.Empty;
            }

            bool full = _root.CombatState != null && _root.CombatState.Rage.IsFull;
            if (_notReadyRemaining > 0f) _notReadyRemaining -= dt;
            if (_rageFillImage != null)
            {
                // Full: pulse between gold and white so READY reads at a glance even in peripheral vision.
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f);
                _rageFillImage.color = full ? Color.Lerp(_rageFullColor, Color.white, pulse * 0.45f) : _rageColor;
            }
            if (_rageFrame != null)
                _rageFrame.color = _notReadyRemaining > 0f ? _rageNotReadyColor : full ? _rageFullColor : _rageFrameColor;
            if (_rageLabel != null) _rageLabel.color = full ? new Color(0.15f, 0.1f, 0f, 1f) : Color.white;
        }

        static string ShortName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return "?";
            int space = displayName.IndexOf(' ');
            return space > 0 ? displayName.Substring(0, space) : displayName;
        }

        static void SetFill(RectTransform fill, float fraction)
        {
            if (fill == null) return;
            var max = fill.anchorMax;
            float f = Mathf.Clamp01(fraction);
            if (Mathf.Approximately(max.x, f)) return;
            max.x = f;
            fill.anchorMax = max;
        }

        static void SetFillVertical(RectTransform fill, float fraction)
        {
            var max = fill.anchorMax;
            max.y = Mathf.Clamp01(fraction);
            fill.anchorMax = max;
        }
    }
}
