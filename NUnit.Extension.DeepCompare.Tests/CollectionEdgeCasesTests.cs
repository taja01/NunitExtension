using NUnit.Framework;

namespace DeepCompare.NUnitExtension.Tests
{
    [TestFixture]
    public class CollectionEdgeCasesTests
    {
        [Test]
        public void NullCollectionVsEmpty_IsDifferent()
        {
            List<int>? expected = null;
            var actual = new List<int>();

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));
            Assert.That(ex.Message, Does.Contain("null"));
        }

        [Test]
        public void ArrayVsList_SameElements_AreEqual()
        {
            var expected = new[] { 1, 2, 3 };
            var actual = new List<int> { 1, 2, 3 };

            Assert.That(actual, Matches.DeeplyWith(expected));
        }

        [Test]
        public void OrderInsensitive_WithDuplicates_RespectsCounts()
        {
            var expected = new List<int> { 1, 2, 2, 3 };
            var actual = new List<int> { 2, 1, 3, 2 };

            // If you later add an order-insensitive option, test here.
            // For now this should fail with default (order-sensitive).
            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));
            Assert.That(ex.Message, Does.Contain("Differences").Or.Contains("["));
        }

        [Test]
        public void Dictionary_MissingKey_Reported()
        {
            var expected = new Dictionary<string, int?> { ["a"] = 1, ["b"] = null };
            var actual = new Dictionary<string, int?> { ["a"] = 1 };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));
            Assert.That(ex.Message, Does.Contain("b").Or.Contains("Count"));
        }

        [Test]
        public void FloatingPoint_NaN_DifferenceReportedOrHandled()
        {
            var expected = new List<double?> { 1.0, double.NaN };
            var actual = new List<double?> { 1.0, double.NaN };

            // Depending on comparator policy, NaN==NaN may be true/false; ensure you define and test behavior.
            Assert.That(expected, Matches.DeeplyWith(actual));
        }
    }
}