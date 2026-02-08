using NUnit.Framework;

namespace DeepCompare.NUnitExtension.Tests
{
    [TestFixture]
    public class CollectionNullableGuidelinesTests
    {
        [Test]
        public void NullElement_AtSameIndex_IsEqual()
        {
            var expected = new List<int?> { 1, null, 3 };
            var actual = new List<int?> { 1, null, 3 };

            Assert.That(expected, Matches.DeeplyWith(actual));
        }

        [Test]
        public void NullElement_Vs_Value_IsReported()
        {
            var expected = new List<int?> { 1, null, 3 };
            var actual = new List<int?> { 1, 2, 3 };

            var ex = Assert.Throws<AssertionException>(() =>
                Assert.That(expected, Matches.DeeplyWith(actual)));

            Assert.That(ex.Message, Does.Contain("[1]"));
            Assert.That(ex.Message, Does.Contain("Expected '2'").Or.Contains("Expected 'null'"));
        }

        [Test]
        public void DifferentLengths_AreReportedAsCountDifference()
        {
            var expected = new List<string?> { "a", "b", null };
            var actual = new List<string?> { "a", "b" };

            var ex = Assert.Throws<AssertionException>(() =>
                Assert.That(expected, Matches.DeeplyWith(actual)));

            Assert.That(ex.Message, Does.Contain("Count"));
        }

        [Test]
        public void NullableDateTime_WithTolerance_PassesWhenWithinDelta()
        {
            var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var expected = new List<DateTime?> { baseTime };
            var actual = new List<DateTime?> { baseTime.AddMilliseconds(500) };

            Assert.That(expected,
                Matches.DeeplyWith(actual)
                    .WithGlobalDateTimeTolerance(TimeSpan.FromSeconds(1))
                    );
        }

        [Test]
        public void BoxedNullableElements_AreComparedCorrectly()
        {
            object? boxedExpected = (int?)5;
            object? boxedActual = (int?)null;

            var ex = Assert.Throws<AssertionException>(() =>
                Assert.That(new[] { boxedExpected }, Matches.DeeplyWith(new[] { boxedActual })));

            Assert.That(ex.Message, Does.Contain("[0]"));
        }
    }
}
