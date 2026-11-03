using System;
using System.Reflection;
using GMR.Core.Combat;
using NUnit.Framework;

namespace GMR.Tests
{
    public sealed class HealthTests
    {
        [Test]
        public void New_IsFull()
        {
            var h = new Health(100f);
            Assert.AreEqual(100f, h.Current);
            Assert.AreEqual(100f, h.Max);
            Assert.AreEqual(1f, h.Fraction);
            Assert.IsFalse(h.IsDepleted);
        }

        [TestCase(0f)]
        [TestCase(-5f)]
        [TestCase(float.NaN)]
        public void NonPositiveMax_Throws(float max)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Health(max));
        }

        [Test]
        public void TakeDamage_ReturnsDealt_AndClampsAtZero()
        {
            var h = new Health(100f);
            Assert.AreEqual(30f, h.TakeDamage(30f));
            Assert.AreEqual(70f, h.Current);
            Assert.AreEqual(0.7f, h.Fraction, 1e-6f);
            Assert.AreEqual(70f, h.TakeDamage(500f), "overkill deals only the remaining HP");
            Assert.AreEqual(0f, h.Current);
            Assert.IsTrue(h.IsDepleted);
            Assert.AreEqual(0f, h.TakeDamage(10f), "no damage below 0");
        }

        [TestCase(0f)]
        [TestCase(-3f)]
        [TestCase(float.NaN)]
        public void TakeDamage_IgnoresNonPositive(float amount)
        {
            var h = new Health(50f);
            Assert.AreEqual(0f, h.TakeDamage(amount));
            Assert.AreEqual(50f, h.Current);
        }

        [Test]
        public void Restore_RefillsAndCanChangeMax()
        {
            var h = new Health(100f);
            h.TakeDamage(100f);
            h.Restore(120f);
            Assert.AreEqual(120f, h.Current);
            Assert.AreEqual(120f, h.Max);
            h.TakeDamage(20f);
            h.Restore(0f);
            Assert.AreEqual(120f, h.Current, "max <= 0 keeps the current maximum");
        }

        [Test]
        public void Mutators_AreNotPublic_OnlyDamageSystemWritesHp()
        {
            foreach (var name in new[] { "TakeDamage", "Restore" })
            {
                var m = typeof(Health).GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Assert.IsNotNull(m, name);
                Assert.IsFalse(m.IsPublic, $"Health.{name} must not be public (AD-16)");
            }
            var current = typeof(Health).GetProperty(nameof(Health.Current));
            Assert.IsFalse(current.SetMethod != null && current.SetMethod.IsPublic, "Health.Current must not have a public setter");
        }
    }
}
