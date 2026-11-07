using System.Collections.Generic;
using GMR.Core.Abilities;
using NUnit.Framework;
using UnityEngine;

namespace GMR.Tests
{
    public sealed class ProjectileConfigTests
    {
        readonly List<Object> _assets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var a in _assets) Object.DestroyImmediate(a);
            _assets.Clear();
        }

        T Make<T>() where T : ScriptableObject
        {
            var t = ScriptableObject.CreateInstance<T>();
            _assets.Add(t);
            return t;
        }

        [Test]
        public void Defaults_AreValid()
        {
            var c = Make<ProjectileConfig>();
            var errors = new List<string>();
            Assert.IsTrue(c.Validate(errors), string.Join("; ", errors));
            Assert.AreEqual(c.Speed * c.Lifetime, c.MaxRange, 1e-5f);
        }

        [Test]
        public void NonPositiveSpeedLifetimeOrRadius_AreInvalid()
        {
            var c = Make<ProjectileConfig>();
            c.Speed = 0f;
            c.Lifetime = -1f;
            c.Radius = 0f;
            var errors = new List<string>();
            Assert.IsFalse(c.Validate(errors));
            Assert.AreEqual(3, errors.Count, string.Join("; ", errors));
        }

        [Test]
        public void NegativePierce_IsInvalid()
        {
            var c = Make<ProjectileConfig>();
            c.Pierce = -1;
            Assert.IsFalse(c.Validate(new List<string>()));
        }

        [Test]
        public void ShotAbility_NeedsAProjectileConfig()
        {
            var shot = Make<AbilityDefinition>();
            shot.HitShape = HitShape.Projectile;
            var errors = new List<string>();
            Assert.IsFalse(shot.Validate(errors));
            StringAssert.Contains("ProjectileConfig", errors[0]);

            shot.Projectile = Make<ProjectileConfig>();
            Assert.IsTrue(shot.Validate(new List<string>()));
        }

        [Test]
        public void ShotAbility_InvalidProjectile_IsReported()
        {
            var shot = Make<AbilityDefinition>();
            shot.HitShape = HitShape.Projectile;
            shot.Projectile = Make<ProjectileConfig>();
            shot.Projectile.Speed = 0f;
            Assert.IsFalse(shot.Validate(new List<string>()));
        }

        [Test]
        public void MeleeShapes_NeedPositiveRange()
        {
            var spin = Make<AbilityDefinition>();
            spin.HitShape = HitShape.Circle;
            spin.Range = 0f;
            Assert.IsFalse(spin.Validate(new List<string>()));
            spin.Range = 2.2f;
            Assert.IsTrue(spin.Validate(new List<string>()));
        }

        [Test]
        public void StatusPayload_FollowsTheAsset()
        {
            var bolt = Make<AbilityDefinition>();
            Assert.IsFalse(bolt.StatusPayload.HasStatus);
            bolt.Status = Make<GMR.Core.Status.StatusDefinition>();
            bolt.StatusStacks = 2;
            Assert.IsTrue(bolt.StatusPayload.HasStatus);
            Assert.AreEqual(2, bolt.StatusPayload.Stacks);
            Assert.IsFalse(bolt.StatusPayload.MaxPotency);
            bolt.StatusMaxPotency = true; // Wrath of Zeus: Shock at maximum chain count
            Assert.IsTrue(bolt.StatusPayload.MaxPotency);
        }

        [Test]
        public void WrathOfZeusAsset_AppliesShockAtMaxPotency()
        {
            var wrath = UnityEditor.AssetDatabase.LoadAssetAtPath<AbilityDefinition>("Assets/Data/Abilities/Ability_Heracles_WrathOfZeus.asset");
            Assert.IsNotNull(wrath);
            Assert.IsTrue(wrath.StatusPayload.HasStatus);
            Assert.AreEqual("shock", wrath.Status.Id);
            Assert.IsTrue(wrath.StatusPayload.MaxPotency);
        }
    }
}
