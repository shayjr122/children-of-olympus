using System;
using System.Collections.Generic;
using GMR.Core.Combat;
using GMR.Core.Enemies;
using GMR.Core.Players;
using GMR.Core.Rng;
using GMR.Core.Status;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GMR.Tests
{
    /// <summary>
    /// A minimal Encounter for status tests: CombatEvents, DamageSystem with the real StatusApplier, an EnemyRegistry,
    /// the RunRng Status stream, and fake enemies that carry StatusContainers. Owns the ScriptableObjects it creates.
    /// </summary>
    public sealed class StatusTestWorld : IDisposable
    {
        public sealed class FakeEnemy : IDamageable, IEnemyTarget
        {
            public FakeEnemy(SourceId id, Vector2 position, float hp, StatusServices services)
            {
                Id = id;
                Position = position;
                Health = new Health(hp);
                Statuses = new StatusContainer(this, services);
            }

            public SourceId Id { get; }
            public bool IsAlive => !Health.IsDepleted;
            public Vector2 Position { get; set; }
            public Health Health { get; }
            public StatusContainer Statuses { get; }
            public float KnockbackResistance => 0f;
            public bool Invulnerable;
            public bool IsInvulnerable(float now) => Invulnerable;
            public void ApplyResolved(in DamageResult result) { }
            public override string ToString() => Id.ToString();
        }

        public static readonly SourceId P0 = SourceId.ForPlayer(new PlayerId(0));

        readonly List<Object> _owned = new List<Object>();

        public StatusTestWorld(ulong seed = 12345)
        {
            Events = new CombatEvents();
            Damage = new DamageSystem(Events);
            Enemies = new EnemyRegistry();
            Rng = new RunRng(seed);
            Services = new StatusServices(Events, Damage, Enemies, Rng.Stream(RngStreams.Status));
            Damage.StatusApplier = new StatusApplier();
            Damage.BeginFrame();
            Events.OnHitLanded += Hits.Add;
            Events.OnConductedShock += Conducted.Add;
            Events.OnChainJumped += Jumps.Add;
            Events.OnStatusApplied += Applied.Add;
            Events.OnStatusExpired += Expired.Add;
        }

        public CombatEvents Events { get; }
        public DamageSystem Damage { get; }
        public EnemyRegistry Enemies { get; }
        public RunRng Rng { get; }
        public StatusServices Services { get; }

        public readonly List<HitLanded> Hits = new List<HitLanded>();
        public readonly List<ConductedShock> Conducted = new List<ConductedShock>();
        public readonly List<ChainJumped> Jumps = new List<ChainJumped>();
        public readonly List<StatusApplied> Applied = new List<StatusApplied>();
        public readonly List<StatusExpired> Expired = new List<StatusExpired>();

        public T Create<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            _owned.Add(o);
            return o;
        }

        public StatusDefinition Status(string id, float duration, int maxStacks = 1, RefreshRule rule = RefreshRule.RefreshDuration,
            params StatusBehaviour[] behaviours)
        {
            var def = Create<StatusDefinition>();
            def.Id = id;
            def.Duration = duration;
            def.MaxStacks = maxStacks;
            def.RefreshRule = rule;
            def.Behaviours = behaviours;
            return def;
        }

        /// <summary>Shock with the story's default chain: N 3, R 3.5, falloff 0.7, 8 damage, 6 jumps at max potency.</summary>
        public StatusDefinition Shock(out ChainLightningBehaviour chain)
        {
            chain = Create<ChainLightningBehaviour>();
            chain.MaxJumps = 3;
            chain.MaxChainJumps = 6;
            chain.Radius = 3.5f;
            chain.Falloff = 0.7f;
            chain.Damage = 8f;
            chain.JumpTags = DamageTags.Shock;
            return Status("shock", 0.6f, 1, RefreshRule.RefreshDuration, chain);
        }

        /// <summary>Rule_WetConductsShock with the approved defaults, installed as the only rule.</summary>
        public StatusInteractionRule WetConductsShock(StatusDefinition shock, StatusDefinition wet)
        {
            var rule = Create<StatusInteractionRule>();
            rule.WhenApplied = shock;
            rule.WhenDamageTagged = DamageTags.Shock;
            rule.TargetHas = wet;
            rule.DamageMultiplier = 2f;
            rule.BonusJumps = 1;
            rule.BonusRadius = 1.5f;
            rule.ConsumeTargetStatus = false;
            rule.ConductedEventTag = "wet_conducts_shock";
            var set = Create<StatusRuleSet>();
            set.Rules = new[] { rule };
            Services.Rules = set;
            return rule;
        }

        public FakeEnemy Enemy(Vector2 position, float hp = 1000f)
        {
            var e = new FakeEnemy(SourceId.ForEnemy(Enemies.NextEnemyId()), position, hp, Services);
            Enemies.Register(e);
            return e;
        }

        /// <summary>Enemies in a line along +x starting at the origin.</summary>
        public List<FakeEnemy> Line(int count, float spacing)
        {
            var list = new List<FakeEnemy>(count);
            for (int i = 0; i < count; i++) list.Add(Enemy(new Vector2(i * spacing, 0f)));
            return list;
        }

        /// <summary>A Thunderbolt-like hit: 8 Shock damage from P0 applying <paramref name="status"/>.</summary>
        public void ShockHit(IDamageable target, StatusDefinition status, float amount = 8f, DamageTags tags = DamageTags.Shock,
            bool maxPotency = false, float now = 1f)
        {
            Damage.Apply(target, new DamageRequest(P0, amount, tags, Vector2.zero, new StatusPayload(status, 1, maxPotency),
                Damage.NextHitId()), now);
        }

        public void Dispose()
        {
            foreach (var o in _owned)
            {
                if (o != null) Object.DestroyImmediate(o);
            }
            _owned.Clear();
        }
    }
}
