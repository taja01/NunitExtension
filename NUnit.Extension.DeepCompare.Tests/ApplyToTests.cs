using NUnit.Framework;

namespace DeepCompare.NUnitExtension.Tests
{
    [TestFixture]
    public class ApplyToTests
    {
        [Test]
        public void ApplyTo_ReturnsSuccess_ForEqualObjects()
        {
            var expected = new ResponseBody { IsSuccess = true, StatusCode = 200 };
            var actual = new ResponseBody { IsSuccess = true, StatusCode = 200 };

            var constraint = Matches.DeeplyWith(expected);
            var result = constraint.ApplyTo(actual);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public void ApplyTo_ReturnsFailure_AndWritesDiagnosticMessage()
        {
            var expected = new ResponseBody { IsSuccess = true, StatusCode = 200 };
            var actual = new ResponseBody { IsSuccess = false, StatusCode = 200 };

            var constraint = Matches.DeeplyWith(expected);
            var result = constraint.ApplyTo(actual);

            Assert.That(result.IsSuccess, Is.False);

            // If the result is our custom result, verify the diagnostic text contains the failing property
            var writer = new TextMessageWriter();
            if (result is DeeplyEqualConstraintResult derr)
            {
                derr.WriteMessageTo(writer);
            }
            else
            {
                writer.WriteMessageLine(result.Description);
            }

            var msg = writer.ToString();
            Assert.That(msg, Does.Contain("IsSuccess"));
            Assert.That(msg, Does.Contain("Expected 'True'").Or.Contains("Expected 'true'"));
        }
    }
}