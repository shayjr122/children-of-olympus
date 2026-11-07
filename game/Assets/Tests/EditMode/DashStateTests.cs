using GMR.Core.Heroes;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    public sealed class DashStateTests
    {
        DashConfig _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<DashConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void Defaults_MatchAddendum()
        {
            Assert.AreEqual(3f, _config.Distance);
            Assert.AreEqual(0.15f, _config.Duration);
            Assert.AreEqual(0.10f, _config.IFrameDuration);
            Assert.AreEqual(0.12f, _config.RecoveryDuration);
            Assert.AreEqual(0.08f, _config.PerfectWindow);
            Assert.AreEqual(20f, _config.Speed, 1e-4f);
        }

        [Test]
        public void Start_FromReady_Dashes_AlongNormalizedDirection()
        {
            var d = new DashState();
            Assert.IsTrue(d.TryStart(new Vector2(0f, 2f), 1f, _config));
            Assert.AreEqual(DashPhase.Dashing, d.Phase);
            Assert.AreEqual(Vector2.up, d.Direction);
            Assert.AreEqual(20f, d.Velocity.magnitude, 1e-4f);
            Assert.AreEqual(1f, d.StartTime);
        }

        [Test]
        public void Phases_DashingThenRecoveryThenReady()
        {
            var d = new DashState();
            d.TryStart(Vector2.right, 0f, _config);
            d.Tick(0.149f);
            Assert.AreEqual(DashPhase.Dashing, d.Phase);
            d.Tick(0.15f);
            Assert.AreEqual(DashPhase.Recovery, d.Phase);
            Assert.AreEqual(Vector2.zero, d.Velocity);
            d.Tick(0.269f);
            Assert.AreEqual(DashPhase.Recovery, d.Phase);
            d.Tick(0.2701f);
            Assert.AreEqual(DashPhase.Ready, d.Phase);
        }

        [Test]
        public void Recovery_BlocksNewDash()
        {
            var d = new DashState();
            d.TryStart(Vector2.right, 0f, _config);
            Assert.IsFalse(d.TryStart(Vector2.left, 0.05f, _config), "no DASH while dashing");
            Assert.IsFalse(d.TryStart(Vector2.left, 0.20f, _config), "no DASH during Dash Recovery");
            Assert.AreEqual(Vector2.right, d.Direction);
            Assert.IsTrue(d.TryStart(Vector2.left, 0.2701f, _config), "DASH allowed once recovery ends (no cooldown)");
        }

        [Test]
        public void RecoveryRemaining_DrainsFromOneToZero()
        {
            var d = new DashState();
            d.TryStart(Vector2.right, 0f, _config);
            Assert.AreEqual(0f, d.RecoveryRemaining01(0.1f));
            d.Tick(0.15f);
            Assert.AreEqual(1f, d.RecoveryRemaining01(0.15f), 1e-4f);
            Assert.AreEqual(0.5f, d.RecoveryRemaining01(0.21f), 1e-3f);
        }

        [Test]
        public void ConfigSnapshot_AtStart_LiveEditsApplyNextDash()
        {
            var d = new DashState();
            d.TryStart(Vector2.right, 0f, _config);
            _config.Duration = 1f;
            d.Tick(0.15f);
            Assert.AreEqual(DashPhase.Recovery, d.Phase);
        }

        [Test]
        public void ZeroDirection_KeepsPreviousDirection()
        {
            var d = new DashState();
            d.TryStart(Vector2.down, 0f, _config);
            d.TryStart(Vector2.zero, 1f, _config);
            Assert.AreEqual(Vector2.down, d.Direction);
        }
    }
}
