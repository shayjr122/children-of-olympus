using GMR.Core.Encounter;
using NUnit.Framework;

namespace GMR.Tests
{
    public sealed class TimeScaleServiceTests
    {
        [Test]
        public void NoRequests_IsOne()
        {
            Assert.AreEqual(1f, new TimeScaleService().Resolve(0f));
        }

        [Test]
        public void LowestScaleWins()
        {
            var s = new TimeScaleService();
            s.Request(0.5f, 1f, 0f);
            s.Request(0.3f, 1f, 0f);
            s.Request(0.8f, 1f, 0f);
            Assert.AreEqual(0.3f, s.Resolve(0.1f));
        }

        [Test]
        public void TimedRequest_ExpiresOnUnscaledTime()
        {
            var s = new TimeScaleService();
            s.Request(0.3f, 0.15f, 10f);
            Assert.AreEqual(0.3f, s.Resolve(10.149f));
            Assert.AreEqual(1f, s.Resolve(10.151f));
            Assert.AreEqual(0, s.ActiveCount);
        }

        [Test]
        public void Pause_IsZero_AndBeatsSlowMo()
        {
            var s = new TimeScaleService();
            s.Request(0.3f, 0.15f, 0f);
            int pause = s.Hold(0f);
            Assert.AreEqual(0f, s.Resolve(0.05f));
            // While paused the slow-mo still expires (unscaled clock), so unpausing returns to full speed.
            Assert.AreEqual(0f, s.Resolve(1f));
            Assert.IsTrue(s.Release(pause));
            Assert.AreEqual(1f, s.Resolve(1f));
        }

        [Test]
        public void Release_UnknownHandle_ReturnsFalse()
        {
            Assert.IsFalse(new TimeScaleService().Release(42));
        }

        [Test]
        public void Release_OnlyRemovesThatRequest()
        {
            var s = new TimeScaleService();
            int a = s.Hold(0.5f);
            s.Hold(0.7f);
            s.Release(a);
            Assert.AreEqual(0.7f, s.Resolve(0f));
        }

        [Test]
        public void ZeroDuration_IsNoOp_NegativeScaleClampedToZero()
        {
            var s = new TimeScaleService();
            s.Request(0.1f, 0f, 0f);
            Assert.AreEqual(1f, s.Resolve(0f));
            s.Request(-2f, 1f, 0f);
            Assert.AreEqual(0f, s.Resolve(0.5f));
            Assert.AreEqual(0f, s.Current);
        }
    }
}
