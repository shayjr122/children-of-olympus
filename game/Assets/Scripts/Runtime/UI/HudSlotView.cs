using System;
using UnityEngine;

namespace GMR.Runtime.UI
{
    /// <summary>One ability-slot box of a PlayerHudPanel (FR-28): button glyph + ability name, cooldown seconds and a
    /// dark overlay that drains as the cooldown runs out. Plain serializable view data; PlayerHudPanel drives it.</summary>
    [Serializable]
    public sealed class HudSlotView
    {
        [Tooltip("Button glyph shown before the name, e.g. 'X'.")] public string Glyph = "X";
        public TMPro.TMP_Text Name;
        public TMPro.TMP_Text Value;

        [Tooltip("Overlay whose top anchor = cooldown remaining fraction (anchored to the box bottom).")]
        public RectTransform CooldownOverlay;

        public UnityEngine.UI.Image Background;

        [NonSerialized] public string LastName;
        [NonSerialized] public int LastTenths = int.MinValue;
        [NonSerialized] public float LastOverlay = -1f;
    }
}
