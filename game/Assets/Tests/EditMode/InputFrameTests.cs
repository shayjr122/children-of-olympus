using GMR.Core.Controls;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    public sealed class InputFrameTests
    {
        [Test]
        public void Default_IsNeutral()
        {
            InputFrame f = default;
            Assert.AreEqual(Vector2.zero, f.Move);
            Assert.AreEqual(Vector2.zero, f.Aim);
            Assert.IsFalse(f.DashPressed);
            Assert.IsFalse(f.BasicPressed);
            Assert.IsFalse(f.BasicHeld);
            Assert.IsFalse(f.SpecialPressed);
            Assert.IsFalse(f.SignaturePressed);
            Assert.IsFalse(f.UltimatePressed);
            Assert.IsFalse(f.DirectionLockHeld);
            Assert.IsFalse(f.HeroSwapPressed);
            Assert.IsFalse(f.PausePressed);
            Assert.IsFalse(f.DebugMenuPressed);
            Assert.IsFalse(f.AnyPressed);
        }

        [Test]
        public void ClearPresses_KeepsAxesAndHeld()
        {
            var f = new InputFrame
            {
                Move = new Vector2(1f, 0f),
                Aim = new Vector2(0f, 1f),
                DashPressed = true,
                BasicPressed = true,
                BasicHeld = true,
                SpecialPressed = true,
                SignaturePressed = true,
                UltimatePressed = true,
                DirectionLockHeld = true,
                HeroSwapPressed = true,
                PausePressed = true,
                DebugMenuPressed = true,
            };
            Assert.IsTrue(f.AnyPressed);

            f.ClearPresses();

            Assert.IsFalse(f.AnyPressed);
            Assert.AreEqual(new Vector2(1f, 0f), f.Move);
            Assert.AreEqual(new Vector2(0f, 1f), f.Aim);
            Assert.IsTrue(f.BasicHeld);
            Assert.IsTrue(f.DirectionLockHeld);
        }

        [Test]
        public void DebugMenuPressed_CountsAsPress()
        {
            var f = new InputFrame { DebugMenuPressed = true };
            Assert.IsTrue(f.AnyPressed);
            f.ClearPresses();
            Assert.IsFalse(f.DebugMenuPressed);
        }
    }
}
