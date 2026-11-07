using System.Collections.Generic;
using GMR.Runtime.Encounter;
using GMR.Runtime.Players;
using UnityEngine;

namespace GMR.Runtime.UI
{
    /// <summary>
    /// Per-Player combat HUD host (FR-28, NFR-2, AD-4): instantiates one PlayerHudPanel from an inactive template for
    /// every Player that joins (EncounterBootstrap.PlayerJoined) and binds it to that PlayerRoot. Panel slots: P1 top-left,
    /// P2 top-right, P3/P4 below them. A panel whose Player left is destroyed. No static lookup.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public sealed class CombatHud : MonoBehaviour
    {
        [SerializeField] EncounterBootstrap _bootstrap;
        [SerializeField, Tooltip("Parent of the spawned panels (stretched over the canvas).")] RectTransform _container;
        [SerializeField, Tooltip("Inactive panel template, anchored top-left.")] PlayerHudPanel _template;
        [SerializeField, Tooltip("Gap to the screen edge, reference pixels (1280x800 canvas).")] Vector2 _margin = new Vector2(8f, 8f);
        [SerializeField, Tooltip("Vertical step between panels on the same side, reference pixels.")] float _rowStep = 180f;

        readonly List<PlayerHudPanel> _panels = new List<PlayerHudPanel>(4);

        public IReadOnlyList<PlayerHudPanel> Panels => _panels;

        void OnEnable()
        {
            if (_bootstrap == null || _template == null || _container == null)
            {
                UnityEngine.Debug.LogError("[GMR/UI] CombatHud needs a bootstrap, a container and a panel template.", this);
                return;
            }
            _bootstrap.PlayerJoined += OnPlayerJoined;
            var players = _bootstrap.PlayerRoots;
            for (int i = 0; i < players.Count; i++) OnPlayerJoined(players[i]);
        }

        void OnDisable()
        {
            if (_bootstrap != null) _bootstrap.PlayerJoined -= OnPlayerJoined;
        }

        /// <summary>The panel bound to <paramref name="root"/>, or null.</summary>
        public PlayerHudPanel PanelFor(PlayerRoot root)
        {
            for (int i = 0; i < _panels.Count; i++)
            {
                if (_panels[i] != null && _panels[i].Root == root) return _panels[i];
            }
            return null;
        }

        void OnPlayerJoined(PlayerRoot root)
        {
            if (root == null || !root.IsInitialized || PanelFor(root) != null) return;
            var panel = Instantiate(_template, _container);
            panel.name = "HudPanel_P" + (root.Id.Value + 1);
            Place((RectTransform)panel.transform, root.Id.Value);
            panel.gameObject.SetActive(true);
            panel.Bind(root, _bootstrap.Context);
            _panels.Add(panel);
        }

        void LateUpdate()
        {
            for (int i = _panels.Count - 1; i >= 0; i--)
            {
                if (_panels[i] != null && _panels[i].Root != null) continue;
                if (_panels[i] != null) Destroy(_panels[i].gameObject);
                _panels.RemoveAt(i);
            }
        }

        void Place(RectTransform rect, int playerIndex)
        {
            bool right = playerIndex % 2 == 1;
            int row = playerIndex / 2;
            var corner = new Vector2(right ? 1f : 0f, 1f);
            rect.anchorMin = rect.anchorMax = rect.pivot = corner;
            rect.anchoredPosition = new Vector2(right ? -_margin.x : _margin.x, -_margin.y - row * _rowStep);
        }
    }
}
