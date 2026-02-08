using NUnit.Framework;

namespace DeepCompare.NUnitExtension.Tests
{
    [TestFixture]
    public class MaxDifferencesTests
    {
        [Test]
        public void DefaultMaxDifferences_IsLimitedTo100()
        {
            // default MaxDifferences is 100, prepare many differences
            var expected = Enumerable.Range(0, 500).Select(i => i).ToList();
            var actual = expected.Select(i => i + 1).ToList(); // all values differ

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));
            Assert.That(ex.Message, Does.Contain("Maximum limit of 100 reached."));
            Assert.That(ex.Message, Does.Contain("Differences found: 100. The details are as follows:"));
        }

        [Test]
        public void WithMaxDifferences_OverridesDefault()
        {
            var expected = Enumerable.Range(0, 50).Select(i => i).ToList();
            var actual = expected.Select(i => i + 10).ToList(); // all values differ

            // override to 5 differences
            var ex = Assert.Throws<AssertionException>(() =>
                Assert.That(actual, Matches.DeeplyWith(expected, o => o.WithMaxDifferences(5))));

            Assert.That(ex.Message, Does.Contain("Differences found: 5. The details are as follows:"));
        }
    }
}