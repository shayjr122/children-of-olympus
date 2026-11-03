namespace GMR.Core.Combat
{
    /// <summary>
    /// Where DamageSystem hands the status payload of a landed hit (AD-9 / AD-10 hook). Epic 3 ships no status logic:
    /// the default applier ignores the payload. Epic 5 plugs the StatusContainer routing in here.
    /// </summary>
    public interface IStatusApplier
    {
        void Apply(IDamageable target, in StatusPayload payload, SourceId source, float now);
    }
}
