using GMR.Core.Combat;
using GMR.Core.Encounter;
using GMR.Core.Feedback;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    public sealed class FeedbackConfigTests
    {
        FeedbackConfig _config;
        TimeScaleService _timeScale;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<FeedbackConfig>();
            _timeScale = new TimeScaleService();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void Defaults_MatchTheTuningTable()
        {
            Assert.AreEqual(0.03f, _config.HitstopLight, 1e-6f);
            Assert.AreEqual(0.08f, _config.HitstopHeavy, 1e-6f);
            Assert.IsTrue(_config.Hitstop);
            Assert.IsTrue(_config.DamageNumbers);
            Assert.IsTrue(_config.ScreenShake);
            Assert.IsTrue(_config.HitFlash);
            Assert.IsTrue(_config.SfxHooks);
            Assert.Greater(_config.HitstopScale, 0f, "hitstop never reaches scale 0 (that is pause)");
        }

        [TestCase(HitWeight.Light)]
        [TestCase(HitWeight.Heavy)]
        public void HitstopZero_MakesNoTimeScaleRequest(HitWeight weight)
        {
            _config.HitstopLight = 0f;
            _config.HitstopHeavy = 0f;
            Assert.IsFalse(HitstopRule.TryRequest(_timeScale, _config, true, weight, 1f));
            Assert.AreEqual(0, _timeScale.ActiveCount);
            Assert.AreEqual(1f, _timeScale.Resolve(1f));
        }

        [Test]
        public void HitstopToggledOff_MakesNoTimeScaleRequest()
        {
            Assert.IsFalse(HitstopRule.TryRequest(_timeScale, _config, enabled: false, HitWeight.Heavy, 1f));
            Assert.AreEqual(0, _timeScale.ActiveCount);
        }

        [Test]
        public void HeavyHit_RequestsHeavyHitstop_ThenExpires()
        {
            Assert.IsTrue(HitstopRule.TryRequest(_timeScale, _config, true, HitWeight.Heavy, 1f));
            Assert.AreEqual(1, _timeScale.ActiveCount);
            Assert.AreEqual(_config.HitstopScale, _timeScale.Resolve(1.05f), 1e-6f);
            Assert.AreEqual(1f, _timeScale.Resolve(1.081f), "0.08 s heavy hitstop is over");
        }

        [Test]
        public void LightHit_UsesTheLightDuration()
        {
            HitstopRule.TryRequest(_timeScale, _config, true, HitWeight.Light, 0f);
            Assert.AreEqual(_config.HitstopScale, _timeScale.Resolve(0.02f), 1e-6f);
            Assert.AreEqual(1f, _timeScale.Resolve(0.031f));
        }

        [Test]
        public void PauseStillWinsOverHitstop()
        {
            int pause = _timeScale.Hold(0f);
            HitstopRule.TryRequest(_timeScale, _config, true, HitWeight.Heavy, 0f);
            Assert.AreEqual(0f, _timeScale.Resolve(0.01f));
            _timeScale.Release(pause);
            Assert.AreEqual(_config.HitstopScale, _timeScale.Resolve(0.02f), 1e-6f);
        }

        [Test]
        public void DamageNumbersToggle_OverrideBeatsConfig()
        {
            var toggles = new SessionToggles();
            Assert.IsTrue(SessionToggles.Resolve(toggles.DamageNumbers, _config.DamageNumbers));
            toggles.DamageNumbers = false;
            Assert.IsFalse(SessionToggles.Resolve(toggles.DamageNumbers, _config.DamageNumbers));
            toggles.ClearOverrides();
            Assert.IsNull(toggles.DamageNumbers);
        }
    }
}
