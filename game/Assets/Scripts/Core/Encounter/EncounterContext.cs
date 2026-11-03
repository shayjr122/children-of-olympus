using GMR.Core.Combat;
using GMR.Core.Enemies;
using GMR.Core.Players;
using GMR.Core.Rng;

namespace GMR.Core.Encounter
{
    /// <summary>
    /// The only service root (AD-5). Created by EncounterBootstrap and passed by hand via Init(...). One per
    /// Encounter; nothing here is static, so state never leaks between Play Mode entries with domain reload off.
    /// </summary>
    public sealed class EncounterContext
    {
        public EncounterContext(ulong runSeed)
        {
            Rng = new RunRng(runSeed);
            Damage = new DamageSystem(Events);
        }

        public RunRng Rng { get; }
        public PlayerRegistry Players { get; } = new PlayerRegistry();
        public EnemyRegistry Enemies { get; } = new EnemyRegistry();
        public CombatEvents Events { get; } = new CombatEvents();
        public DamageSystem Damage { get; }
        public TimeScaleService TimeScale { get; } = new TimeScaleService();
        public PendingHitRegistry PendingHits { get; } = new PendingHitRegistry();
        public SessionToggles Toggles { get; } = new SessionToggles();
    }
}
