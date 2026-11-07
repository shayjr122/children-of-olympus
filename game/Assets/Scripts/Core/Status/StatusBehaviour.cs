using GMR.Core.Combat;
using UnityEngine;

namespace GMR.Core.Status
{
    /// <summary>
    /// One reusable piece of status logic (AD-10), referenced from <see cref="StatusDefinition.Behaviours"/>. Variants are
    /// ScriptableObject subclasses (never [SerializeReference]). Adding a behaviour type = one new subclass; the
    /// <see cref="StatusContainer"/> calls these hooks generically and never knows concrete types. Behaviours are shared
    /// assets: keep per-target state in the <see cref="StatusInstance"/> or in objects created per application.
    /// Damage a behaviour deals goes through <see cref="StatusServices.Damage"/> (tag it StatusTick or ChainJump so it
    /// counts for Combo and RAGE at the status rate).
    /// </summary>
    public abstract class StatusBehaviour : ScriptableObject
    {
        /// <summary>Every application, new or refresh (after the refresh rule ran).</summary>
        public virtual void OnApply(in StatusApplyContext c) { }

        /// <summary>Once per StatusTicker frame while active, with scaled dt.</summary>
        public virtual void OnTick(in StatusTickContext c) { }

        /// <summary>When the instance ends (expired, consumed by a rule or cleared).</summary>
        public virtual void OnExpire(in StatusExpireContext c) { }

        /// <summary>
        /// Part of DamageSystem's modifier fold for damage the carrier receives (status modifiers first, in container
        /// order, then interaction rules). Flat adds before multipliers.
        /// </summary>
        public virtual void ModifyIncoming(in StatusModifyContext c, ref float amount, ref ChainParams chain, in DamageRequest request) { }
    }

    /// <summary>Why a status instance ended.</summary>
    public enum StatusEndReason
    {
        Expired = 0,
        Consumed = 1,
        Cleared = 2,
    }

    /// <summary>What the application that triggered OnApply carried (from the hit, when applied by DamageSystem).</summary>
    public readonly struct StatusApplyInfo
    {
        /// <summary>Chain modifiers the hit's fold produced on this target (Wet: +1 jump, +1.5 u).</summary>
        public readonly ChainParams ChainBonus;

        /// <summary>True if an interaction rule matched this hit on this target (Conducted).</summary>
        public readonly bool Conducted;

        /// <summary>Apply at maximum potency (Wrath of Zeus: Shock at maximum chain count).</summary>
        public readonly bool MaxPotency;

        /// <summary>The chain this application belongs to (a chain jump), or null.</summary>
        public readonly ChainRun Chain;

        public readonly DamageTags HitTags;
        public readonly int HitId;

        public StatusApplyInfo(ChainParams chainBonus, bool conducted, bool maxPotency, ChainRun chain, DamageTags hitTags, int hitId)
        {
            ChainBonus = chainBonus;
            Conducted = conducted;
            MaxPotency = maxPotency;
            Chain = chain;
            HitTags = hitTags;
            HitId = hitId;
        }
    }

    public readonly struct StatusApplyContext
    {
        public readonly StatusContainer Container;
        public readonly StatusInstance Instance;
        public readonly float Now;
        public readonly bool IsNew;
        public readonly int AppliedStacks;
        public readonly StatusApplyInfo Info;

        public StatusApplyContext(StatusContainer container, StatusInstance instance, float now, bool isNew, int appliedStacks,
            in StatusApplyInfo info)
        {
            Container = container;
            Instance = instance;
            Now = now;
            IsNew = isNew;
            AppliedStacks = appliedStacks;
            Info = info;
        }

        public IDamageable Target => Container.Owner;
        public StatusServices Services => Container.Services;
        public SourceId Source => Instance.Source;
        public StatusDefinition Definition => Instance.Definition;
    }

    public readonly struct StatusTickContext
    {
        public readonly StatusContainer Container;
        public readonly StatusInstance Instance;
        public readonly float Dt;
        public readonly float Now;

        public StatusTickContext(StatusContainer container, StatusInstance instance, float dt, float now)
        {
            Container = container;
            Instance = instance;
            Dt = dt;
            Now = now;
        }

        public IDamageable Target => Container.Owner;
        public StatusServices Services => Container.Services;
        public SourceId Source => Instance.Source;
    }

    public readonly struct StatusExpireContext
    {
        public readonly StatusContainer Container;
        public readonly StatusInstance Instance;
        public readonly StatusEndReason Reason;
        public readonly float Now;

        public StatusExpireContext(StatusContainer container, StatusInstance instance, StatusEndReason reason, float now)
        {
            Container = container;
            Instance = instance;
            Reason = reason;
            Now = now;
        }

        public IDamageable Target => Container.Owner;
        public StatusServices Services => Container.Services;
    }

    public readonly struct StatusModifyContext
    {
        public readonly StatusContainer Container;
        public readonly StatusInstance Instance;
        public readonly float Now;

        public StatusModifyContext(StatusContainer container, StatusInstance instance, float now)
        {
            Container = container;
            Instance = instance;
            Now = now;
        }

        public IDamageable Target => Container.Owner;
    }
}
