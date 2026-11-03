using System.Text;
using GMR.Core.Abilities;
using GMR.Runtime.Encounter;
using UnityEngine;

namespace GMR.Runtime.UI
{
    /// <summary>
    /// Minimal per-Player readout (FR-28 stub until the combat HUD in Story 4.3): Hero HP and the remaining cooldown
    /// of Special and Signature, one line per joined Player. Read-only view; the Text is only reassigned when the shown text changes.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class AbilityCooldownReadout : MonoBehaviour
    {
        [SerializeField] EncounterBootstrap _bootstrap;
        [SerializeField] UnityEngine.UI.Text _text;

        readonly StringBuilder _sb = new StringBuilder(128);
        string _last = string.Empty;

        void LateUpdate()
        {
            if (_bootstrap == null || _text == null) return;
            _sb.Clear();
            float now = Time.time;
            var players = _bootstrap.PlayerRoots;
            for (int i = 0; i < players.Count; i++)
            {
                var hero = players[i].Hero;
                if (hero == null || !hero.IsInitialized) continue;
                if (_sb.Length > 0) _sb.Append('\n');
                _sb.Append(players[i].Id.ToString()).Append(' ').Append(hero.Definition.DisplayName);
                if (hero.Health != null)
                    _sb.Append("   HP ").Append(Mathf.CeilToInt(hero.Health.Current)).Append('/').Append(Mathf.CeilToInt(hero.Health.Max));
                AppendSlot(hero.Abilities?.Get(AbilitySlot.Special), "Special", now);
                AppendSlot(hero.Abilities?.Get(AbilitySlot.Signature), "Signature", now);
            }
            string text = _sb.ToString();
            if (text == _last) return;
            _last = text;
            _text.text = text;
        }

        void AppendSlot(AbilityRunner runner, string label, float now)
        {
            if (runner == null) return;
            _sb.Append("   ").Append(label).Append(": ");
            float remaining = runner.CooldownRemaining(now);
            if (remaining <= 0f) _sb.Append("READY");
            else _sb.Append(remaining.ToString("0.0")).Append('s');
        }
    }
}
