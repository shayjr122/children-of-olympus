using GMR.Core.Status;

namespace GMR.Core.Combat
{
    /// <summary>The status a hit applies (AD-8, AD-9). <see cref="Def"/> null means none.</summary>
    public readonly struct StatusPayload
    {
        public readonly StatusDefinition Def;
        public readonly int Stacks;

        public StatusPayload(StatusDefinition def, int stacks)
        {
            Def = def;
            Stacks = stacks < 1 ? 1 : stacks;
        }

        public bool HasStatus => Def != null;
    }
}
