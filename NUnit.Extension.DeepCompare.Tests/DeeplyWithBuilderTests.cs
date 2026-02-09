using NUnit.Framework;

namespace DeepCompare.NUnitExtension.Tests
{
    [TestFixture]
    public class DeeplyWithBuilderTests
    {
        [Test]
        public void ImplicitConversion_BackCompat_WorksSameAsBefore()
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

            // DeeplyWith now returns a builder but has implicit conversion to the Constraint.
            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));
            Assert.That(ex.Message, Does.Contain("Property 'Method'"));
        }

        [Test]
        public void Fluent_Skip_AllowsIgnoring_PropertyDifference()
        {
            var actual = new ResponseBody
            {
                StatusCode = 200,
                Method = Method.GET
            };

            var expected = new ResponseBody
            {
                StatusCode = 200,
                Method = null
            };

            // Skip the 'Method' property using the fluent builder
            Assert.DoesNotThrow(() => Assert.That(actual, Matches.DeeplyWith(expected).Skip("Method")));
        }

        [Test]
        public void Fluent_WithGlobalDateTimeTolerance_AllowsSmallDelta()
        {
            var now = DateTime.UtcNow;
            var actual = new TimeHolder { CreatedAt = now };
            var expected = new TimeHolder { CreatedAt = now.AddMilliseconds(500) }; // 500ms difference

            // Global tolerance 1 second => should pass
            Assert.DoesNotThrow(() =>
                Assert.That(actual, Matches.DeeplyWith(expected).WithGlobalDateTimeTolerance(TimeSpan.FromSeconds(1))));
        }

        [Test]
        public void Fluent_PerPropertyDateTimeTolerance_OverridesGlobalTolerance()
        {
            var now = DateTime.UtcNow;
            var actual = new TimeHolder { CreatedAt = now };
            var expected = new TimeHolder { CreatedAt = now.AddSeconds(2) }; // 2s difference

            // Global tolerance 1s (would fail), but per-property tolerance 3s for CreatedAt should allow it
            Assert.DoesNotThrow(() =>
                Assert.That(actual,
                    Matches.DeeplyWith(expected)
                        .WithGlobalDateTimeTolerance(TimeSpan.FromSeconds(1))
                        .WithDateTimeTolerance(nameof(TimeHolder.CreatedAt), TimeSpan.FromSeconds(3))));
        }

        [Test]
        public void Fluent_PerPropertyTimeSpanTolerance_OverridesGlobalTolerance()
        {
            var actual = new TimeHolder { Tolerance = TimeSpan.FromSeconds(3) };
            var expected = new TimeHolder { Tolerance = TimeSpan.FromSeconds(4) };

            Assert.DoesNotThrow(() =>
                Assert.That(actual,
                    Matches.DeeplyWith(expected)
                        .WithGlobalDateTimeTolerance(TimeSpan.FromSeconds(1))
                        .WithDateTimeTolerance(nameof(TimeHolder.CreatedAt), TimeSpan.FromSeconds(3))));
        }

        [Test]
        public void Fluent_PerPropertyTimeSpanNullableTolerance_OverridesGlobalTolerance()
        {
            var actual = new TimeHolder { ToleranceNullable = TimeSpan.FromSeconds(2) };
            var expected = new TimeHolder { ToleranceNullable = TimeSpan.FromSeconds(5) };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual,
                    Matches.DeeplyWith(expected)
                        .WithGlobalDateTimeTolerance(TimeSpan.FromSeconds(1))
                        .WithDateTimeTolerance(nameof(TimeHolder.CreatedAt), TimeSpan.FromSeconds(3))));
            Assert.That(ex.Message, Does.Contain("Property 'ToleranceNullable' mismatch: Expected '00:00:05', but was '00:00:02'."));
        }

        [Test]
        public void TimeSpanTest()
        {
            var actual = new TimeHolder { Tolerance = TimeSpan.FromSeconds(3) };
            var expected = new TimeHolder { Tolerance = TimeSpan.FromSeconds(3) };

            Assert.DoesNotThrow(() => Assert.That(actual, Matches.DeeplyWith(expected)));
        }

        [Test]
        public void Build_ReturnsConstraint_AndCanBeUsedDirectly()
        {
            var actual = new ResponseBody { IsSuccess = true, Message = "A" };
            var expected = new ResponseBody { IsSuccess = true, Message = "B" };

            var constraint = Matches.DeeplyWith(expected).Skip(nameof(ResponseBody.Message));

            // Because we skipped Message, the objects are considered equal
            Assert.DoesNotThrow(() => Assert.That(actual, constraint));
        }

        // Helper type for DateTime tests
        private class TimeHolder
        {
            public DateTime CreatedAt { get; set; }

            public TimeSpan Tolerance { get; set; }

            public TimeSpan? ToleranceNullable { get; set; }
        }
    }
}