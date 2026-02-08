using NUnit.Framework;

namespace DeepCompare.NUnitExtension.Tests
{
    [TestFixture]
    public class DictionaryTests
    {
        [Test]
        public void Dictionary_EqualDictionaries_Pass()
        {
            var actual = new Dictionary<string, int?> { ["a"] = 1, ["b"] = 2, ["c"] = null };
            var expected = new Dictionary<string, int?> { ["a"] = 1, ["b"] = 2, ["c"] = null };

            Assert.That(actual, Matches.DeeplyWith(expected));
        }

        [Test]
        public void Dictionary_MissingKey_Reported()
        {
            var actual = new Dictionary<string, int?> { ["a"] = 1 };
            var expected = new Dictionary<string, int?> { ["a"] = 1, ["b"] = null };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));
            Assert.That(ex.Message, Does.Contain("Differences found: 2. The details are as follows:"));
            Assert.That(ex.Message, Does.Contain("Property 'Count' mismatch: Expected 'Count 2', but was 'Count 1'."));
            Assert.That(ex.Message, Does.Contain("Property '[b]' mismatch: Expected 'null', but was 'null'."));
        }

        [Test]
        public void Dictionary_ExtraKey_Reported()
        {
            var actual = new Dictionary<string, int?> { ["a"] = 1, ["x"] = 9 };
            var expected = new Dictionary<string, int?> { ["a"] = 1 };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));
            Assert.That(ex.Message, Does.Contain("x").Or.Contains("Count"));
        }

        [Test]
        public void Dictionary_ValueDifference_ReportedWithKeyPath()
        {
            var actual = new Dictionary<string, string?> { ["k1"] = "v1", ["k2"] = "actual" };
            var expected = new Dictionary<string, string?> { ["k1"] = "v1", ["k2"] = "expected" };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));
            Assert.That(ex.Message, Does.Contain("[k2]").Or.Contains("k2"));
            Assert.That(ex.Message, Does.Contain("Expected 'expected'").And.Contains("but was 'actual'"));
        }

        [Test]
        public void Dictionary_NullValueVsValue_IsReported()
        {
            var actual = new Dictionary<string, int?> { ["n"] = 5 };
            var expected = new Dictionary<string, int?> { ["n"] = null };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));
            Assert.That(ex.Message, Does.Contain("Differences found: 1. The details are as follows:"));
            Assert.That(ex.Message, Does.Contain("Property '[n]' mismatch: Expected 'null', but was '5'."));
        }

        [Test]
        public void Dictionary_NestedValueDifference_ReportsFullPath()
        {
            var actual = new Dictionary<string, ValueHolder>
            {
                ["item"] = new ValueHolder { Id = 1, Name = "A" }
            };
            var expected = new Dictionary<string, ValueHolder>
            {
                ["item"] = new ValueHolder { Id = 1, Name = "B" }
            };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));
            // Expect a path that includes the dictionary key and the nested property, e.g. "[item].Name" or similar.
            Assert.That(ex.Message, Does.Contain("Differences found: 1. The details are as follows:"));
            Assert.That(ex.Message, Does.Contain("Property '[item].Name' mismatch: Expected 'B', but was 'A'."));
        }

        private class ValueHolder
        {
            public int Id { get; set; }
            public string? Name { get; set; }
        }
    }
}