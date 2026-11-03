using System;
using GMR.Core.Encounter;
using GMR.Core.Players;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    public sealed class PlayerRegistryTests
    {
        sealed class FakePlayer : IPlayerTarget
        {
            public FakePlayer(int id) => Id = new PlayerId(id);
            public PlayerId Id { get; }
            public bool IsAlive => true;
            public Vector2 Position => Vector2.zero;
            public float Radius => 0.4f;
            public bool IsInvulnerable(float now) => false;
            public GMR.Core.Combat.IDamageable Damageable => null;
        }

        [Test]
        public void TwoPlayers_RegisterIndependently()
        {
            var r = new PlayerRegistry();
            var a = new FakePlayer(0);
            var b = new FakePlayer(1);
            r.Register(a);
            r.Register(b);
            Assert.AreEqual(2, r.Count);
            Assert.IsTrue(r.TryGet(new PlayerId(1), out var found));
            Assert.AreSame(b, found);
            Assert.IsTrue(r.Unregister(a));
            Assert.AreEqual(1, r.Count);
        }

        [Test]
        public void DuplicateId_Throws()
        {
            var r = new PlayerRegistry();
            r.Register(new FakePlayer(0));
            Assert.Throws<InvalidOperationException>(() => r.Register(new FakePlayer(0)));
        }

        [Test]
        public void EncounterContexts_DoNotShareState()
        {
            var a = new EncounterContext(1);
            var b = new EncounterContext(1);
            a.Players.Register(new FakePlayer(0));
            a.Toggles.AimAssist = false;
            Assert.AreEqual(0, b.Players.Count);
            Assert.IsNull(b.Toggles.AimAssist);
        }

        [Test]
        public void SessionToggles_OverrideWinsOverConfigDefault()
        {
            Assert.IsTrue(SessionToggles.Resolve(null, true));
            Assert.IsFalse(SessionToggles.Resolve(false, true));
            Assert.IsTrue(SessionToggles.Resolve(true, false));
        }
    }
}
