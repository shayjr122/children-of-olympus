using System;
using GMR.Core.Rng;
using NUnit.Framework;

namespace GMR.Tests
{
    public sealed class RunRngTests
    {
        const ulong Seed = 12345UL;

        static uint[] Draw(IRng rng, int count)
        {
            var values = new uint[count];
            for (int i = 0; i < count; i++) values[i] = rng.NextUInt();
            return values;
        }

        [Test]
        public void SameSeed_SameSequence()
        {
            var a = Draw(new RunRng(Seed).Stream(RngStreams.Waves), 100);
            var b = Draw(new RunRng(Seed).Stream(RngStreams.Waves), 100);
            CollectionAssert.AreEqual(a, b);
        }

        [Test]
        public void DifferentSeed_DifferentSequence()
        {
            var a = Draw(new RunRng(Seed).Stream(RngStreams.Waves), 16);
            var b = Draw(new RunRng(Seed + 1).Stream(RngStreams.Waves), 16);
            CollectionAssert.AreNotEqual(a, b);
        }

        [Test]
        public void StreamsIndependent()
        {
            var expected = Draw(new RunRng(Seed).Stream(RngStreams.Waves), 100);

            var rng = new RunRng(Seed);
            Draw(rng.Stream(RngStreams.Combat), 1000);
            var actual = Draw(rng.Stream(RngStreams.Waves), 100);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void DifferentStreams_DifferentSequences()
        {
            var rng = new RunRng(Seed);
            CollectionAssert.AreNotEqual(Draw(rng.Stream(RngStreams.Waves), 16), Draw(rng.Stream(RngStreams.Spawns), 16));
        }

        [Test]
        public void Stream_ReturnsSameInstance()
        {
            var rng = new RunRng(Seed);
            Assert.AreSame(rng.Stream(RngStreams.Ai), rng.Stream(RngStreams.Ai));
        }

        [Test]
        public void Derive_StableRegardlessOfOtherChildrenAndDraws()
        {
            var expected = Draw(new RunRng(Seed).Stream(RngStreams.Ai).Derive("chaser", 3), 50);

            var ai = new RunRng(Seed).Stream(RngStreams.Ai);
            for (int i = 0; i < 10; i++) Draw(ai.Derive("chaser", i == 3 ? 99 : i), 7);
            Draw(ai.Derive("tank", 0), 7);
            Draw(ai, 500);
            var actual = Draw(ai.Derive("chaser", 3), 50);

            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void Derive_DifferentIndex_DifferentSequence()
        {
            var ai = new RunRng(Seed).Stream(RngStreams.Ai);
            CollectionAssert.AreNotEqual(Draw(ai.Derive("chaser", 3), 16), Draw(ai.Derive("chaser", 4), 16));
        }

        [Test]
        public void Value01_InHalfOpenUnitRange()
        {
            var s = new RunRng(Seed).Stream(RngStreams.Combat);
            for (int i = 0; i < 10000; i++)
            {
                float v = s.Value01();
                Assert.That(v, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
            }
        }

        [Test]
        public void RangeInt_StaysInBounds_AndHitsEveryValue()
        {
            var s = new RunRng(Seed).Stream(RngStreams.Combat);
            var seen = new bool[7];
            for (int i = 0; i < 5000; i++)
            {
                int v = s.Range(-3, 4);
                Assert.That(v, Is.InRange(-3, 3));
                seen[v + 3] = true;
            }
            CollectionAssert.DoesNotContain(seen, false);
        }

        [Test]
        public void RangeInt_InvalidBounds_Throws()
        {
            var s = new RunRng(Seed).Stream(RngStreams.Combat);
            Assert.Throws<ArgumentOutOfRangeException>(() => s.Range(5, 5));
        }

        [Test]
        public void RangeFloat_StaysInBounds()
        {
            var s = new RunRng(Seed).Stream(RngStreams.Combat);
            for (int i = 0; i < 5000; i++)
                Assert.That(s.Range(2f, 3f), Is.GreaterThanOrEqualTo(2f).And.LessThan(3f));
        }

        [Test]
        public void Fnv1a64_IsStable()
        {
            // Pinned values: if these change, every saved/shared seed changes meaning.
            Assert.AreEqual(14695981039346656037UL, RngHash.Fnv1a64(string.Empty));
            Assert.AreEqual(RngHash.Fnv1a64(RngStreams.Waves), RngHash.Fnv1a64("waves"));
            Assert.AreNotEqual(RngHash.Fnv1a64("waves"), RngHash.Fnv1a64("spawns"));
        }
    }
}
