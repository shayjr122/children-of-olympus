using GMR.Core.Heroes;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    public sealed class MoveMathTests
    {
        MoveConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<MoveConfig>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void Defaults_MatchAddendum()
        {
            Assert.AreEqual(5.5f, _config.MaxSpeed);
            Assert.AreEqual(0.06f, _config.AccelerationTime);
            Assert.AreEqual(0.2f, _config.DeadZone);
        }

        [TestCase(0.1f)]
        [TestCase(0.15f)]
        [TestCase(0.2f)]
        public void DeadZone_AtOrBelow_NoMovement(float deflection)
        {
            Assert.AreEqual(Vector2.zero, MoveMath.ApplyDeadZone(new Vector2(deflection, 0f), 0.2f));
            Assert.AreEqual(Vector2.zero, MoveMath.TargetVelocity(new Vector2(0f, -deflection), _config));
        }

        [Test]
        public void DeadZone_FullDeflection_IsFullMagnitude_AndKeepsDirection()
        {
            Vector2 diagonal = new Vector2(1f, 1f).normalized;
            Vector2 result = MoveMath.ApplyDeadZone(diagonal, 0.2f);
            Assert.AreEqual(1f, result.magnitude, 1e-5f);
            Assert.AreEqual(diagonal.x, result.x, 1e-5f);
            Assert.AreEqual(diagonal.y, result.y, 1e-5f);
        }

        [Test]
        public void DeadZone_IsRescaled_SoHalfwayAboveDeadZoneIsHalfSpeed()
        {
            // 0.6 is halfway between 0.2 and 1.0.
            Assert.AreEqual(0.5f, MoveMath.ApplyDeadZone(new Vector2(0.6f, 0f), 0.2f).magnitude, 1e-5f);
        }

        [Test]
        public void FullDeflection_ReachesMaxSpeed_WithinAccelerationTime()
        {
            Vector2 target = MoveMath.TargetVelocity(Vector2.right, _config);
            Assert.AreEqual(5.5f, target.magnitude, 1e-5f);

            // 60 Hz steps: max speed reached no later than the step that crosses 0.06 s.
            const float dt = 1f / 60f;
            Vector2 v = Vector2.zero;
            float t = 0f;
            while (v.magnitude < _config.MaxSpeed - 1e-4f && t < 1f)
            {
                v = MoveMath.Step(v, target, _config, dt);
                t += dt;
            }
            Assert.That(t, Is.LessThanOrEqualTo(_config.AccelerationTime + dt + 1e-5f));
            Assert.AreEqual(5.5f, v.magnitude, 1e-4f);
        }

        [Test]
        public void Acceleration_IsGradual_NotInstant()
        {
            Vector2 target = MoveMath.TargetVelocity(Vector2.right, _config);
            Vector2 v = MoveMath.Step(Vector2.zero, target, _config, 0.02f);
            // 0.02 s of a 0.06 s ramp = one third of max speed.
            Assert.AreEqual(5.5f / 3f, v.magnitude, 1e-4f);
        }

        [Test]
        public void ZeroAccelerationTime_Snaps()
        {
            _config.AccelerationTime = 0f;
            Vector2 target = new Vector2(0f, 5.5f);
            Assert.AreEqual(target, MoveMath.Step(Vector2.zero, target, _config, 0.001f));
        }

        [Test]
        public void ReleasingStick_DeceleratesToRest()
        {
            Vector2 v = new Vector2(5.5f, 0f);
            for (int i = 0; i < 10; i++) v = MoveMath.Step(v, Vector2.zero, _config, 1f / 60f);
            Assert.AreEqual(Vector2.zero, v);
        }

        [Test]
        public void MaxSpeedChange_AppliesOnNextStep()
        {
            _config.MaxSpeed = 8f;
            Assert.AreEqual(8f, MoveMath.TargetVelocity(Vector2.up, _config).magnitude, 1e-5f);
        }
    }
}
