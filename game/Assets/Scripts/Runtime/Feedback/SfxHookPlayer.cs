using System;
using GMR.Core.Feedback;
using UnityEngine;

namespace GMR.Runtime.Feedback
{
    /// <summary>
    /// Placeholder SFX hooks (FR-27): FeedbackRouter fires a cue per event; this plays the clip assigned to that cue
    /// (none are assigned yet, so it is silent) and can log cues. <see cref="CueFired"/> lets tests and later audio
    /// code listen without touching gameplay.
    /// </summary>
    public sealed class SfxHookPlayer : MonoBehaviour
    {
        [Serializable]
        struct CueClip
        {
            public SfxCue Cue;
            public AudioClip Clip;
            [Range(0f, 1f), Tooltip("0 is treated as 1 (unset); use a tiny value to make a cue very quiet.")] public float Volume;
        }

        [SerializeField] FeedbackConfig _config;
        [SerializeField, Tooltip("Optional AudioSource used for one-shots; silent if empty.")] AudioSource _source;
        [SerializeField, Tooltip("Clip per cue. Empty = silent hook.")] CueClip[] _clips = Array.Empty<CueClip>();

        /// <summary>Raised for every fired cue (instance event).</summary>
        public event Action<SfxCue, Vector2> CueFired;

        public void Fire(SfxCue cue, Vector2 position)
        {
            if (_config != null && !_config.SfxHooks) return;
            CueFired?.Invoke(cue, position);
            if (_config != null && _config.LogSfxCues) UnityEngine.Debug.Log($"[GMR/Audio] SFX cue {cue} at {position}");
            if (_source == null) return;
            for (int i = 0; i < _clips.Length; i++)
            {
                if (_clips[i].Cue != cue || _clips[i].Clip == null) continue;
                _source.PlayOneShot(_clips[i].Clip, _clips[i].Volume <= 0f ? 1f : _clips[i].Volume);
                return;
            }
        }
    }
}
