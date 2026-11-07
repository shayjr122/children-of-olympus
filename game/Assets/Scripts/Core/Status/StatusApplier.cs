using GMR.Core.Combat;

namespace GMR.Core.Status
{
    /// <summary>
    /// DamageSystem's status hook (AD-9/AD-10), replacing the Epic 3 no-op: a landed hit's status payload is applied to the
    /// target's StatusContainer, attributed to the hit's source, with what the fold produced (chain bonus, conducted) and
    /// what the payload carries (max potency, the chain it belongs to). Targets without a container ignore payloads.
    /// </summary>
    public sealed class StatusApplier : IStatusApplier
    {
        public void Apply(IDamageable target, in DamageRequest request, in DamageResult result)
        {
            var container = target?.Statuses;
            var payload = request.Status;
            if (container == null || !payload.HasStatus) return;
            var info = new StatusApplyInfo(result.Chain, result.Conducted, payload.MaxPotency, payload.Chain, request.Tags,
                request.HitId);
            container.Apply(payload.Def, payload.Stacks, request.Source, result.At, info);
        }
    }
}
