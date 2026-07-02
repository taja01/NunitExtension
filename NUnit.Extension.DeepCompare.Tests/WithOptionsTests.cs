using NUnit.Framework;

namespace DeepCompare.NUnitExtension.Tests
{
    [TestFixture]
    public class WithOptionsTests
    {
        [Test]
        public void WithOptions_SkipProperty_AllowsIgnoringDifference()
        {
            var actual = new ResponseBody
            {
                StatusCode = 200,
                IsSuccess = true,
                Numbers = [1, 2, 3],
                Method = Method.GET
            };

            var expected = new ResponseBody
            {
                StatusCode = 200,
                IsSuccess = true,
                Numbers = [1, 2, 3],
                Method = null
            };

            // Use WithOptions to skip the Method property — assertion should not throw.
            Assert.DoesNotThrow(() =>
                Assert.That(actual, Matches.DeeplyWith(expected).WithOptions(o => o.Skip(nameof(ResponseBody.Method)))));
        }

        [Test]
        public void WithOptions_WithMaxDifferences_StopsAfterLimit()
        {
            var expected = Enumerable.Range(0, 50).ToList();
            var actual = expected.Select(x => x + 1).ToList(); // every item different

            var ex = Assert.Throws<AssertionException>(() =>
                Assert.That(actual, Matches.DeeplyWith(expected).WithOptions(o => o.WithMaxDifferences(3))));

            Assert.That(ex.Message, Does.Contain("Differences found: 3. The details are as follows:"));
            Assert.That(ex.Message, Does.Contain("Property '[0]' mismatch: Expected '0', but was '1'."));
            Assert.That(ex.Message, Does.Contain("Property '[1]' mismatch: Expected '1', but was '2'."));
            Assert.That(ex.Message, Does.Contain("Property '[2]' mismatch: Expected '2', but was '3'."));
        }

        [Test]
        public void WithOptions_CombinesSkipAndDateTimeTolerance()
        {
            var expected = new
            {
                Timestamp = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                Name = "Ada"
            };

            var actual = new
            {
                Timestamp = new DateTime(2024, 1, 1, 0, 0, 3, DateTimeKind.Utc),
                Name = "Grace"
            };

            Assert.DoesNotThrow(() =>
                Assert.That(actual, Matches.DeeplyWith(expected).WithOptions(o =>
                {
                    o.Skip("Name");
                    o.WithDateTimeTolerance("Timestamp", TimeSpan.FromSeconds(5));
                })));
        }
    }
}