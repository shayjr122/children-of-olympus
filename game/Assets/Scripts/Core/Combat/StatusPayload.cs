using GMR.Core.Status;

namespace GMR.Core.Combat
{
    /// <summary>
    /// The status a hit applies (AD-8, AD-9). <see cref="Def"/> null means none. <see cref="MaxPotency"/> applies it at
    /// maximum strength (Wrath of Zeus: Shock at maximum chain count); <see cref="Chain"/> is set on chain-lightning jumps
    /// so the landing continues that chain instead of starting a new one.
    /// </summary>
    public readonly struct StatusPayload
    {
        public readonly StatusDefinition Def;
        public readonly int Stacks;
        public readonly bool MaxPotency;
        public readonly ChainRun Chain;

        public StatusPayload(StatusDefinition def, int stacks, bool maxPotency = false, ChainRun chain = null)
        {
            Def = def;
            Stacks = stacks < 1 ? 1 : stacks;
            MaxPotency = maxPotency;
            Chain = chain;
        }

        public bool HasStatus => Def != null;
    }
}
