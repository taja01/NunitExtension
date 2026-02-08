using NUnit.Framework;

namespace DeepCompare.NUnitExtension.Tests
{
    [TestFixture]
    public class ComprehensiveTests
    {
        [Test]
        public void PrimitiveEquality_Passes()
        {
            var actual = 5;
            var expected = 5;

            Assert.That(actual, Matches.DeeplyWith(expected));
        }

        [Test]
        public void TypeMismatch_ReportsDifferentType()
        {
            var actual = new object[] { 1 };
            var expected = new List<object> { 1 };

            Assert.That(actual, Matches.DeeplyWith(expected));
        }

        [Test]
        public void Collection_NullElement_Vs_Value_IsReported_WithIndex()
        {
            var expected = new List<int?> { 1, null, 3 }; // expected null at index 1
            var actual = new List<int?> { 1, 2, 3 };     // actual has 2 at index 1

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));
            Assert.That(ex.Message, Does.Contain("[1]"));
            Assert.That(ex.Message, Does.Contain("Expected 'null'").And.Contains("but was '2'"));
        }

        [Test]
        public void Collection_CountDifference_Reported()
        {
            var expected = new List<string?> { "a", "b", null, "extra" };
            var actual = new List<string?> { "a", "b", null };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));
            Assert.That(ex.Message, Does.Contain("Count"));
            Assert.That(ex.Message, Does.Contain("Count 4").Or.Contains("Count 3"));
        }

        [Test]
        public void NullableDateTime_GlobalTolerance_AllowsSmallDelta()
        {
            var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var expected = new TimeHolder { CreatedAt = baseTime.AddMilliseconds(500) };
            var actual = new TimeHolder { CreatedAt = baseTime };

            Assert.That(actual,
                Matches.DeeplyWith(expected)
                    .WithGlobalDateTimeTolerance(TimeSpan.FromSeconds(1)));
        }

        [Test]
        public void NullableDateTime_PerPropertyTolerance_OverridesGlobal()
        {
            var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var expected = new TimeHolder { CreatedAt = baseTime.AddSeconds(2) };
            var actual = new TimeHolder { CreatedAt = baseTime };

            // Global 1s would fail, per-property 3s allows this
            Assert.That(actual,
                Matches.DeeplyWith(expected)
                    .WithGlobalDateTimeTolerance(TimeSpan.FromSeconds(1))
                    .WithDateTimeTolerance(nameof(TimeHolder.CreatedAt), TimeSpan.FromSeconds(3)));
        }

        [Test]
        public void SkipProperty_AllowsIgnoringDifferences()
        {
            var actual = new ResponseBody { StatusCode = 200, Method = Method.GET };
            var expected = new ResponseBody { StatusCode = 200, Method = null };

            // Without skip this would fail; with Skip it passes
            Assert.That(actual, Matches.DeeplyWith(expected).Skip(nameof(ResponseBody.Method)));
        }

        [Test]
        public void NestedPropertyDifference_IsReportedWithFullPath()
        {
            var actual = new ResponseBody
            {
                StatusCode = 200,
                InnerMessage = new InnerMessage { Message = "Done" }
            };
            var expected = new ResponseBody
            {
                StatusCode = 200,
                InnerMessage = new InnerMessage { Message = "Waiting" }
            };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));
            Assert.That(ex.Message, Does.Contain("InnerMessage.Message"));
            Assert.That(ex.Message, Does.Contain("Expected 'Waiting'").And.Contains("but was 'Done'"));
        }

        [Test]
        public void BoxedNullableElement_Vs_Null_IsReported()
        {
            object? boxedExpected = (int?)5;
            object? boxedActual = (int?)null;

            var ex = Assert.Throws<AssertionException>(() =>
                Assert.That(new[] { boxedActual }, Matches.DeeplyWith(new[] { boxedExpected })));

            Assert.That(ex.Message, Does.Contain("[0]"));
        }

        [Test]
        public void RepeatedReference_SameInstanceAppearingMultiplePlaces_Handled()
        {
            var shared = new InnerMessage { Message = "X" };

            var actual = new TwoRefHolder { A = shared, B = shared };
            var expectedShared = new InnerMessage { Message = "Y" };
            var expected = new TwoRefHolder { A = expectedShared, B = expectedShared };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));
            Assert.That(ex.Message, Does.Contain("Message").And.Contains("Expected 'Y'").And.Contains("but was 'X'"));
        }

        [Test]
        public void CyclicGraph_DoesNotStackOverflow_AndReportsDifferences()
        {
            var a1 = new Node("A1");
            var b1 = new Node("B1");
            a1.Next = b1;
            b1.Next = a1; // cycle

            var a2 = new Node("A1");
            var b2 = new Node("B2"); // different name here
            a2.Next = b2;
            b2.Next = a2; // cycle

            var ex = Assert.Throws<AssertionException>(() => Assert.That(a1, Matches.DeeplyWith(a2)));
            // It should detect the difference in B's name and not blow up
            Assert.That(ex.Message, Does.Contain("Next").And.Contains("Name").And.Contains("B2"));
        }

        // Helper types used only in tests
        private class TimeHolder
        {
            public DateTime? CreatedAt { get; set; }
        }

        private class TwoRefHolder
        {
            public InnerMessage? A { get; set; }
            public InnerMessage? B { get; set; }
        }

        private class Node
        {
            public string Name { get; set; }
            public Node? Next { get; set; }
            public Node(string name) => Name = name;
        }
    }
}