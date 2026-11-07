using GMR.Core.Controls;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    public sealed class InputBufferTests
    {
        InputConfig _config;
        InputBuffer _buffer;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<InputConfig>();
            _buffer = new InputBuffer(_config);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void Defaults_MatchAddendum()
        {
            Assert.AreEqual(0.12f, _config.BufferWindow);
            Assert.IsTrue(_config.BufferEnabled);
            Assert.AreEqual(0.12f, _buffer.Window);
        }

        [Test]
        public void PressInsideWindow_IsConsumed()
        {
            _buffer.Press(BufferedAction.Dash, 1.00f);
            Assert.IsTrue(_buffer.TryConsume(BufferedAction.Dash, 1.10f));
        }

        [Test]
        public void PressOutsideWindow_IsDropped()
        {
            _buffer.Press(BufferedAction.Dash, 1.00f);
            Assert.IsFalse(_buffer.TryConsume(BufferedAction.Dash, 1.13f));
            Assert.IsFalse(_buffer.TryConsume(BufferedAction.Dash, 1.13f));
        }

        [Test]
        public void Press_IsConsumedOnlyOnce()
        {
            _buffer.Press(BufferedAction.Dash, 2f);
            Assert.IsTrue(_buffer.TryConsume(BufferedAction.Dash, 2.05f));
            Assert.IsFalse(_buffer.TryConsume(BufferedAction.Dash, 2.06f));
        }

        [Test]
        public void WindowZero_DisablesBuffering_SameFramePressStillWorks()
        {
            _config.BufferWindow = 0f;
            _buffer.Press(BufferedAction.Dash, 3f);
            Assert.IsFalse(_buffer.TryConsume(BufferedAction.Dash, 3.016f), "a press from an earlier frame must not be buffered");

            _buffer.Press(BufferedAction.Dash, 4f);
            Assert.IsTrue(_buffer.TryConsume(BufferedAction.Dash, 4f), "a press in the frame the action is legal still executes");
        }

        [Test]
        public void DisabledToggle_ActsLikeWindowZero()
        {
            _buffer.Enabled = false;
            Assert.AreEqual(0f, _buffer.Window);
            _buffer.Press(BufferedAction.Dash, 1f);
            Assert.IsFalse(_buffer.TryConsume(BufferedAction.Dash, 1.05f));
        }

        [Test]
        public void WindowIsReadLive_FromConfig()
        {
            _buffer.Press(BufferedAction.Dash, 0f);
            _config.BufferWindow = 0.3f;
            Assert.IsTrue(_buffer.TryConsume(BufferedAction.Dash, 0.25f));
        }

        [Test]
        public void Actions_AreIndependent()
        {
            _buffer.Press(BufferedAction.Basic, 1f);
            Assert.IsFalse(_buffer.TryConsume(BufferedAction.Dash, 1f));
            Assert.IsTrue(_buffer.IsBuffered(BufferedAction.Basic, 1.05f));
            Assert.IsTrue(_buffer.TryConsume(BufferedAction.Basic, 1.05f));
        }

        [Test]
        public void DashPressedDuringRecovery_ExecutesWhenRecoveryEnds_IfWithinWindow()
        {
            // Story 2.3: recovery ends at 0.27 s; a press at 0.20 s (0.07 s early) executes then.
            _buffer.Press(BufferedAction.Dash, 0.20f);
            Assert.IsTrue(_buffer.TryConsume(BufferedAction.Dash, 0.27f));
        }

        [Test]
        public void Clear_DropsEverything()
        {
            _buffer.Press(BufferedAction.Dash, 1f);
            _buffer.Clear();
            Assert.IsFalse(_buffer.TryConsume(BufferedAction.Dash, 1f));
        }
    }
}
