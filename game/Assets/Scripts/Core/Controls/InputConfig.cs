using UnityEngine;

namespace GMR.Core.Controls
{
    /// <summary>Input tuning (FR-8, AD-7). Read live each frame.</summary>
    [CreateAssetMenu(menuName = "GMR/Config/Input Config", fileName = "Config_Input")]
    public sealed class InputConfig : ScriptableObject
    {
        [SerializeField, Min(0f), Tooltip("Seconds a press stays buffered before its action becomes legal. 0 disables buffering.")]
        float _bufferWindow = 0.12f;

        [SerializeField, Tooltip("Input buffering default; the pause menu can override it per session.")]
        bool _bufferEnabled = true;

        public float BufferWindow { get => _bufferWindow; internal set => _bufferWindow = value; }
        public bool BufferEnabled { get => _bufferEnabled; internal set => _bufferEnabled = value; }
    }
}
