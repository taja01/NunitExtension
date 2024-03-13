using NUnit.Framework;

namespace DeepCompare.NUnitExtension.Tests
{
    [TestFixture]
    public class MultipleDifferencesTests
    {
        [Test]
        public void TopLevelTest()
        {
            var response1 = new ResponseBody
            {
                IsSuccess = true,
                Message = "Accepted",
                Method = Method.POST,
                StatusCode = 202
            };

            var response2 = new ResponseBody
            {
                IsSuccess = false,
                Message = "Bad request",
                Method = Method.GET,
                StatusCode = 400
            };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(response1, Matches.DeeplyWith(response2)));

            Assert.That(ex.Message, Does.Contain("Differences found: 4. The details are as follows:"));
            Assert.That(ex.Message, Does.Contain("Property 'StatusCode' mismatch: Expected '400', but was '202'."));
            Assert.That(ex.Message, Does.Contain("Property 'IsSuccess' mismatch: Expected 'False', but was 'True'."));
            Assert.That(ex.Message, Does.Contain("Property 'Message' mismatch: Expected 'Bad request', but was 'Accepted'."));
            Assert.That(ex.Message, Does.Contain("Property 'Method' mismatch: Expected 'GET', but was 'POST'."));
        }

        [Test]
        public void LowerLevelTest()
        {
            var response1 = new ResponseBody
            {
                IsSuccess = true,
                Message = "Accepted",
                Method = Method.POST,
                StatusCode = 202,
                InnerMessage = new InnerMessage { Message = "Test" }
            };

            var response2 = new ResponseBody
            {
                IsSuccess = true,
                Message = "Accepted",
                Method = Method.POST,
                StatusCode = 200,
                InnerMessage = new InnerMessage { Message = "Dev" }
            };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(response1, Matches.DeeplyWith(response2)));

            Assert.That(ex.Message, Does.Contain("Differences found: 2. The details are as follows:"));
            Assert.That(ex.Message, Does.Contain("Property 'StatusCode' mismatch: Expected '200', but was '202'."));
            Assert.That(ex.Message, Does.Contain("Property 'InnerMessage.Message' mismatch: Expected 'Dev', but was 'Test'."));
        }

        [Test]
        public void LowerLevelTest2()
        {
            var response1 = new ResponseBody
            {
                IsSuccess = true,
                Message = "Accepted",
                Method = Method.POST,
                StatusCode = 202,
                InnerMessage = new InnerMessage { Message = "Test" },
                Numbers = [1, 2, 3, 4, 5, 6]
            };

            var response2 = new ResponseBody
            {
                IsSuccess = true,
                Message = "Accepted",
                Method = Method.POST,
                StatusCode = 200,
                InnerMessage = new InnerMessage { Message = "Dev" },
                Numbers = [7, 6, 5, 4, 3, 2, 1]
            };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(response1, Matches.DeeplyWith(response2)));

            Assert.That(ex.Message, Does.Contain("Differences found: 3. The details are as follows:"));
            Assert.That(ex.Message, Does.Contain("Property 'StatusCode' mismatch: Expected '200', but was '202'."));
            Assert.That(ex.Message, Does.Contain("Property 'Numbers.Count' mismatch: Expected 'Count 7', but was 'Count 6'."));
            Assert.That(ex.Message, Does.Contain("Property 'InnerMessage.Message' mismatch: Expected 'Dev', but was 'Test'."));
        }
    }
}
