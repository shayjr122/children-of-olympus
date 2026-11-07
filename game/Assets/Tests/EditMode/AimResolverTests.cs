using System.Collections.Generic;
using GMR.Core.Controls;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    public sealed class AimResolverTests
    {
        AimConfig _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<AimConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        static Vector2 AtAngle(float degrees, float distance) =>
            new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad)) * distance;

        static void AssertDir(Vector2 expected, Vector2 actual) =>
            Assert.That(Vector2.Distance(expected.normalized, actual), Is.LessThan(1e-4f), $"expected {expected.normalized}, got {actual}");

        [Test]
        public void Defaults_MatchAddendum()
        {
            Assert.AreEqual(AimMode.MovementAim, _config.Mode);
            Assert.IsTrue(_config.AssistEnabled);
            Assert.AreEqual(25f, _config.AssistHalfAngle);
            Assert.AreEqual(8f, _config.AssistRange);
            Assert.IsTrue(_config.DirectionLockEnabled);
        }

        [Test]
        public void StickNeutral_KeepsLastNonZeroDirection()
        {
            var aim = new AimResolver();
            aim.Update(new Vector2(0f, 0.7f), false, true);
            aim.Update(Vector2.zero, false, true);
            aim.Update(Vector2.zero, false, true);
            AssertDir(Vector2.up, aim.Direction);
        }

        [Test]
        public void Direction_IsNormalized_AndFollowsMovement()
        {
            var aim = new AimResolver();
            aim.Update(new Vector2(-0.3f, 0.3f), false, true);
            AssertDir(new Vector2(-1f, 1f), aim.Direction);
            Assert.AreEqual(1f, aim.Direction.magnitude, 1e-5f);
        }

        [Test]
        public void Assist_On_CandidateAt15DegInCone_Snaps()
        {
            var aim = new AimResolver(Vector2.right);
            var target = AtAngle(15f, 5f);
            Vector2 fire = aim.ResolveFire(Vector2.zero, new List<Vector2> { target }, _config, assistOn: true);
            AssertDir(target, fire);
        }

        [Test]
        public void Assist_Off_DoesNotSnap()
        {
            var aim = new AimResolver(Vector2.right);
            Vector2 fire = aim.ResolveFire(Vector2.zero, new List<Vector2> { AtAngle(15f, 5f) }, _config, assistOn: false);
            AssertDir(Vector2.right, fire);
        }

        [Test]
        public void Assist_CandidateAt30DegOutsideCone_Unchanged()
        {
            var aim = new AimResolver(Vector2.right);
            Vector2 fire = aim.ResolveFire(Vector2.zero, new List<Vector2> { AtAngle(30f, 5f) }, _config, assistOn: true);
            AssertDir(Vector2.right, fire);
        }

        [Test]
        public void Assist_CandidateOutOfRange_Unchanged()
        {
            var aim = new AimResolver(Vector2.right);
            Vector2 fire = aim.ResolveFire(Vector2.zero, new List<Vector2> { AtAngle(5f, 9f) }, _config, assistOn: true);
            AssertDir(Vector2.right, fire);
        }

        [Test]
        public void Assist_NoCandidates_Unchanged()
        {
            var aim = new AimResolver(Vector2.up);
            AssertDir(Vector2.up, aim.ResolveFire(Vector2.zero, new List<Vector2>(), _config, true));
        }

        [Test]
        public void Assist_PicksNearestInCone_RelativeToOrigin()
        {
            var aim = new AimResolver(Vector2.right);
            var origin = new Vector2(10f, -2f);
            var far = origin + AtAngle(5f, 7f);
            var near = origin + AtAngle(-20f, 3f);
            var outside = origin + AtAngle(90f, 1f);
            int index;
            Assert.IsTrue(AimResolver.TryFindAssistTarget(origin, aim.Direction, new List<Vector2> { far, near, outside }, 25f, 8f, out index));
            Assert.AreEqual(1, index);
        }

        [Test]
        public void Lock_Held_FreezesDirection_WhileWalkingElsewhere()
        {
            var aim = new AimResolver();
            aim.Update(Vector2.right, false, true);
            aim.Update(Vector2.up, true, true);   // press: frozen at right
            aim.Update(Vector2.left, true, true);
            Assert.IsTrue(aim.IsLocked);
            AssertDir(Vector2.right, aim.Direction);
        }

        [Test]
        public void Lock_Released_ResumesOnNextMovementInput()
        {
            var aim = new AimResolver();
            aim.Update(Vector2.right, false, true);
            aim.Update(Vector2.up, true, true);
            aim.Update(Vector2.zero, false, true); // released, no movement yet
            Assert.IsFalse(aim.IsLocked);
            AssertDir(Vector2.right, aim.Direction);
            aim.Update(Vector2.down, false, true); // next movement input
            AssertDir(Vector2.down, aim.Direction);
        }

        [Test]
        public void Lock_FeatureDisabled_ButtonDoesNothing()
        {
            var aim = new AimResolver();
            aim.Update(Vector2.right, false, false);
            aim.Update(Vector2.up, true, false);
            Assert.IsFalse(aim.IsLocked);
            AssertDir(Vector2.up, aim.Direction);
        }

        [Test]
        public void Lock_FiresAlongLockedDirection_AssistStillUsesLockedAim()
        {
            var aim = new AimResolver();
            aim.Update(Vector2.right, false, true);
            aim.Update(Vector2.left, true, true);
            var target = AtAngle(10f, 4f);
            AssertDir(target, aim.ResolveFire(Vector2.zero, new List<Vector2> { target }, _config, true));
        }
    }
}
