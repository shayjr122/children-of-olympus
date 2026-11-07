using GMR.Runtime.Encounter;
using UnityEngine;

namespace GMR.Runtime.Status
{
    /// <summary>
    /// The only thing that ticks statuses (AD-16): once per frame, every StatusContainer registered in the Encounter's
    /// StatusRegistry is ticked exactly once with scaled time (frozen while paused, slowed by hitstop). Runs after
    /// gameplay (abilities, players) so a status applied this frame starts ticking next frame.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public sealed class StatusTicker : MonoBehaviour
    {
        [SerializeField] EncounterBootstrap _bootstrap;

        /// <summary>Frames ticked so far (diagnostics and tests).</summary>
        public int FramesTicked { get; private set; }

        void Awake()
        {
            if (_bootstrap == null)
            {
                UnityEngine.Debug.LogError("[GMR/Status] StatusTicker needs an EncounterBootstrap.", this);
                enabled = false;
            }
        }

        void Update()
        {
            _bootstrap.Context.Statuses.TickAll(Time.deltaTime, Time.time);
            FramesTicked++;
        }
    }
}
