using GMR.Runtime.Controls;
using UnityEngine;

namespace GMR.Runtime.Players
{
    /// <summary>
    /// M3 placeholder: moves the Sandbox square from PlayerInputReader.Frame.Move. Replaced by HeroMover in M4a.
    /// </summary>
    public sealed class SandboxMover : MonoBehaviour
    {
        [SerializeField] PlayerInputReader _reader;
        [SerializeField, Min(0f), Tooltip("Units per second (1 unit = 32 px).")]
        float _speed = 5f;

        void Reset() => _reader = GetComponent<PlayerInputReader>();

        void Update()
        {
            if (_reader == null) return;
            Vector2 move = _reader.Frame.Move;
            transform.position += (Vector3)(move * (_speed * Time.deltaTime));
        }
    }
}
