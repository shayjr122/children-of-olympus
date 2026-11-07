using System;
using System.Collections.Generic;
using GMR.Core.Combat;

namespace GMR.Core.Status
{
    /// <summary>
    /// The statuses on one damageable (FR-18, AD-10), one container per entity (<see cref="IDamageable.Statuses"/>).
    /// Stores instances with their source, applies refresh/stack rules, ticks them (only via StatusTicker, AD-16), runs
    /// behaviours through the generic <see cref="StatusBehaviour"/> hooks and folds incoming damage: status behaviours in
    /// container order, then the <see cref="StatusRuleSet"/>'s interaction rules (flat adds before multipliers).
    /// Sole raiser of StatusApplied, StatusExpired and ConductedShock. Knows no concrete status or behaviour.
    /// </summary>
    public sealed class StatusContainer
    {
        const float ExpiryEpsilon = 1e-4f;

        readonly List<StatusInstance> _instances = new List<StatusInstance>(4);
        readonly List<StatusInstance> _tickSnapshot = new List<StatusInstance>(4);
        readonly List<StatusInstance> _clearSnapshot = new List<StatusInstance>(4);
        readonly List<StatusInteractionRule> _consumed = new List<StatusInteractionRule>(2);

        public StatusContainer(IDamageable owner, StatusServices services)
        {
            Owner = owner ?? throw new ArgumentNullException(nameof(owner));
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        public IDamageable Owner { get; }
        public StatusServices Services { get; }

        /// <summary>Active instances in application order.</summary>
        public IReadOnlyList<StatusInstance> Instances => _instances;

        public int Count => _instances.Count;

        /// <summary>How many times <see cref="Tick"/> ran (diagnostics: StatusTicker ticks each container once per frame).</summary>
        public int TickCount { get; private set; }

        public bool Has(StatusDefinition definition) => Find(definition) != null;

        public bool TryGet(StatusDefinition definition, out StatusInstance instance)
        {
            instance = Find(definition);
            return instance != null;
        }

        /// <summary>Applies <paramref name="definition"/> from <paramref name="source"/> (debug tools, environment).</summary>
        public StatusInstance Apply(StatusDefinition definition, int stacks, SourceId source, float now) =>
            Apply(definition, stacks, source, now, default);

        /// <summary>
        /// Applies a status: a new instance, or the definition's refresh rule on the existing one. Raises StatusApplied and
        /// then runs every behaviour's OnApply (which may chain, deal damage, apply other statuses).
        /// </summary>
        public StatusInstance Apply(StatusDefinition definition, int stacks, SourceId source, float now, in StatusApplyInfo info)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            stacks = Math.Max(1, stacks);
            var instance = Find(definition);
            bool isNew = instance == null;
            if (isNew)
            {
                instance = new StatusInstance(definition, source, Math.Min(definition.MaxStacks, stacks), now);
                _instances.Add(instance);
            }
            else
            {
                switch (definition.RefreshRule)
                {
                    case RefreshRule.RefreshDuration:
                        Refresh(instance, source, now);
                        instance.Stacks = Math.Min(definition.MaxStacks, instance.Stacks);
                        break;
                    case RefreshRule.AddStacks:
                        Refresh(instance, source, now);
                        instance.Stacks = Math.Min(definition.MaxStacks, instance.Stacks + stacks);
                        break;
                    case RefreshRule.KeepExisting:
                        break;
                }
            }

            Services.Events.RaiseStatusApplied(new StatusApplied(Owner, Owner.Id, Owner.Position, definition, source,
                instance.Stacks, isNew, now));

            var behaviours = definition.Behaviours;
            var context = new StatusApplyContext(this, instance, now, isNew, stacks, info);
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] != null) behaviours[i].OnApply(context);
            }
            return instance;
        }

        /// <summary>Ends <paramref name="definition"/> now (OnExpire runs, StatusExpired raised). False if not active.</summary>
        public bool Remove(StatusDefinition definition, float now, StatusEndReason reason = StatusEndReason.Cleared)
        {
            var instance = Find(definition);
            if (instance == null) return false;
            End(instance, reason, now);
            return true;
        }

        /// <summary>Ends every status (target died or respawned).</summary>
        public void Clear(float now)
        {
            _clearSnapshot.Clear();
            _clearSnapshot.AddRange(_instances);
            for (int i = 0; i < _clearSnapshot.Count; i++)
            {
                if (!_clearSnapshot[i].IsRemoved) End(_clearSnapshot[i], StatusEndReason.Cleared, now);
            }
            _clearSnapshot.Clear();
        }

        /// <summary>Advances every instance by scaled <paramref name="dt"/>: OnTick, then expiry. Called by StatusTicker only.</summary>
        public void Tick(float dt, float now)
        {
            TickCount++;
            if (_instances.Count == 0) return;
            // Snapshot: behaviours may deal damage (fold, consume), apply statuses or kill the owner (Clear) meanwhile.
            _tickSnapshot.Clear();
            _tickSnapshot.AddRange(_instances);
            for (int i = 0; i < _tickSnapshot.Count; i++)
            {
                var instance = _tickSnapshot[i];
                if (instance.IsRemoved) continue;
                instance.Remaining -= dt;
                instance.Elapsed += dt;
                var behaviours = instance.Definition.Behaviours;
                var context = new StatusTickContext(this, instance, dt, now);
                for (int b = 0; b < behaviours.Length && !instance.IsRemoved; b++)
                {
                    if (behaviours[b] != null) behaviours[b].OnTick(context);
                }
                if (!instance.IsRemoved && instance.Remaining <= ExpiryEpsilon) End(instance, StatusEndReason.Expired, now);
            }
            _tickSnapshot.Clear();
        }

        /// <summary>
        /// DamageSystem's modifier fold for a hit this container's owner receives (AD-9, AD-16): status behaviours'
        /// ModifyIncoming in container order, then matching interaction rules (chain flat adds, then damage multipliers).
        /// Raises ConductedShock per rule that fires with a tag, and consumes target statuses if the rule says so.
        /// Returns true if any rule fired (a "conducted" hit).
        /// </summary>
        public bool FoldIncoming(ref float amount, ref ChainParams chain, in DamageRequest request, float now)
        {
            if (_instances.Count == 0) return false;

            // ModifyIncoming must not add or remove statuses (consumption is a rule's job, applied after the loop).
            for (int i = 0; i < _instances.Count; i++)
            {
                var instance = _instances[i];
                var behaviours = instance.Definition.Behaviours;
                var context = new StatusModifyContext(this, instance, now);
                for (int b = 0; b < behaviours.Length; b++)
                {
                    if (behaviours[b] != null) behaviours[b].ModifyIncoming(context, ref amount, ref chain, request);
                }
            }

            var rules = Services.Rules != null ? Services.Rules.Rules : null;
            if (rules == null || rules.Length == 0) return false;

            bool fired = false;
            float multiplier = 1f;
            _consumed.Clear();
            for (int i = 0; i < rules.Length; i++)
            {
                var rule = rules[i];
                if (rule == null || rule.TargetHas == null || !Has(rule.TargetHas)) continue;
                if (!rule.Matches(request, out bool tagMatched)) continue;
                fired = true;
                chain.MaxJumps += rule.BonusJumps;
                chain.Radius += rule.BonusRadius;
                chain.Falloff += rule.BonusFalloff;
                float ruleMultiplier = tagMatched ? rule.DamageMultiplier : 1f;
                multiplier *= ruleMultiplier;
                if (rule.ConductedEventTag.Length > 0)
                {
                    Services.Events.RaiseConductedShock(new ConductedShock(Owner, Owner.Id, Owner.Position, request.Source,
                        rule.ConductedEventTag, rule.TargetHas, ruleMultiplier, request.Tags, request.HitId, now));
                }
                if (rule.ConsumeTargetStatus) _consumed.Add(rule);
            }
            amount = Math.Max(0f, amount * multiplier);

            for (int i = 0; i < _consumed.Count; i++) Remove(_consumed[i].TargetHas, now, StatusEndReason.Consumed);
            _consumed.Clear();
            return fired;
        }

        static void Refresh(StatusInstance instance, SourceId source, float now)
        {
            instance.Remaining = instance.Definition.Duration;
            instance.Source = source;
            instance.LastAppliedAt = now;
        }

        void End(StatusInstance instance, StatusEndReason reason, float now)
        {
            if (instance.IsRemoved) return;
            instance.IsRemoved = true;
            _instances.Remove(instance);
            var behaviours = instance.Definition.Behaviours;
            var context = new StatusExpireContext(this, instance, reason, now);
            for (int b = 0; b < behaviours.Length; b++)
            {
                if (behaviours[b] != null) behaviours[b].OnExpire(context);
            }
            Services.Events.RaiseStatusExpired(new StatusExpired(Owner, Owner.Id, instance.Definition, instance.Source, reason, now));
        }

        StatusInstance Find(StatusDefinition definition)
        {
            if (definition == null) return null;
            for (int i = 0; i < _instances.Count; i++)
            {
                if (_instances[i].Definition == definition) return _instances[i];
            }
            return null;
        }
    }
}
