namespace GMR.Core.Combat
{
    /// <summary>
    /// Where DamageSystem hands the status payload of a landed hit (AD-9 / AD-10 hook), after HP and hit events, with the
    /// resolved result (chain bonus from the fold). The Encounter uses GMR.Core.Status.StatusApplier.
    /// </summary>
    public interface IStatusApplier
    {
        void Apply(IDamageable target, in DamageRequest request, in DamageResult result);
    }
}
