using System;
using GMR.Core.Combat;
using GMR.Core.Combo;
using GMR.Core.Rage;

namespace GMR.Core.Players
{
    /// <summary>
    /// Per-person combat state (AD-6): one Player's ComboCounter and RageMeter, hosted by PlayerRoot so it survives a
    /// Hero Swap (it holds no HP). Subscribes to CombatEvents and reacts only to its own Player (AD-11): hits it dealt
    /// (Combo +1; RAGE at the hit, status-tick/chain or Ultimate rate; Combo milestones), damage it took (Combo reset)
    /// and its Perfect Dodges (RAGE). HitAvoided is deliberately not handled: it neither resets Combo nor grants RAGE.
    /// </summary>
    public sealed class PlayerCombatState
    {
        readonly CombatEvents _events;
        bool _bound;

        public PlayerCombatState(PlayerId owner, ComboConfig combo, RageConfig rage, CombatEvents events)
        {
            Owner = owner;
            _events = events ?? throw new ArgumentNullException(nameof(events));
            Combo = new ComboCounter(owner, combo, events);
            Rage = new RageMeter(owner, rage, events);
        }

        public PlayerId Owner { get; }
        public ComboCounter Combo { get; }
        public RageMeter Rage { get; }

        /// <summary>Starts listening to CombatEvents (idempotent).</summary>
        public void Bind()
        {
            if (_bound) return;
            _bound = true;
            _events.OnHitLanded += HandleHitLanded;
            _events.OnPlayerDamaged += HandlePlayerDamaged;
            _events.OnPerfectDodged += HandlePerfectDodged;
        }

        /// <summary>Stops listening (idempotent). Called when the PlayerRoot goes away.</summary>
        public void Unbind()
        {
            if (!_bound) return;
            _bound = false;
            _events.OnHitLanded -= HandleHitLanded;
            _events.OnPlayerDamaged -= HandlePlayerDamaged;
            _events.OnPerfectDodged -= HandlePerfectDodged;
        }

        /// <summary>Advances the Combo window and RAGE decay by scaled <paramref name="dt"/>.</summary>
        public void Tick(float dt)
        {
            Combo.Tick(dt);
            Rage.Tick(dt);
        }

        /// <summary>Which RAGE rate a landed hit with <paramref name="tags"/> fills at.</summary>
        public static RageSource RageSourceFor(DamageTags tags)
        {
            if ((tags & DamageTags.Ultimate) != 0) return RageSource.UltimateHit;
            if ((tags & (DamageTags.StatusTick | DamageTags.ChainJump)) != 0) return RageSource.StatusTick;
            return RageSource.Hit;
        }

        void HandleHitLanded(HitLanded e)
        {
            if (!Combo.Handle(e)) return;
            var source = RageSourceFor(e.Result.Tags);
            Rage.Add(source);
            // Milestones reached by the Ultimate's own hits grant nothing: an Ultimate never refills RAGE by itself.
            if (source != RageSource.UltimateHit && Rage.Config.IsComboMilestone(Combo.Count)) Rage.Add(RageSource.ComboMilestone);
        }

        void HandlePlayerDamaged(PlayerDamaged e) => Combo.Handle(e);

        void HandlePerfectDodged(PerfectDodged e)
        {
            if (e.Player == Owner) Rage.Add(RageSource.PerfectDodge);
        }
    }
}
