using NUnit.Framework;

namespace NUnit.Extension.DeepCompare.Tests
{
    [TestFixture]
    public class RequestBodyTests
    {
        [Test]
        public void EmptyTest()
        {
            var actual = new ResponseBody();
            var expected = new ResponseBody();

            Assert.That(actual, Matches.DeeplyWith(expected));
        }

        [Test]
        public void Test1()
        {
            var actual = new ResponseBody { IsSuccess = true };
            var expected = new ResponseBody { IsSuccess = true };

            Assert.That(actual, Matches.DeeplyWith(expected));
        }

        [Test]
        public void Test2()
        {
            var actual = new ResponseBody { IsSuccess = true };
            var expected = new ResponseBody { IsSuccess = false };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));

            Assert.That(ex.Message, Does.Contain("Property 'IsSuccess' mismatch: Expected 'False', but was 'True'."));
        }

        [Test]
        public void Test3()
        {
            var actual = new ResponseBody { IsSuccess = true, Strings = new List<string> { "22", "44" } };
            var expected = new ResponseBody { IsSuccess = true, Strings = new List<string> { "22", "34" } };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));

            Assert.That(ex.Message, Does.Contain("Property 'Strings.[1].Chars' mismatch: Expected '34', but was '44'."));
        }

        [Test]
        public void Test4()
        {
            var actual = new ResponseBody { IsSuccess = true, Numbers = new List<int> { 1, 2, 3 } };
            var expected = new ResponseBody { IsSuccess = true, Numbers = new List<int> { 3, 2, 1 } };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));

            Assert.That(ex.Message, Does.Contain("Property 'Numbers.[0].' mismatch: Expected '3', but was '1'."));
        }

        [Test]
        public void Test5()
        {
            var actual = new ResponseBody
            {
                StatusCode = 200,
                IsSuccess = true,
                Numbers = new List<int> { 1, 2, 3 },
                Method = Method.GET
            };
            var expected = new ResponseBody
            {
                StatusCode = 200,
                IsSuccess = true,
                Numbers = new List<int> { 1, 2, 3 },
            };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));

            Assert.That(ex.Message, Does.Contain("Property 'Method' mismatch: Expected 'null', but was 'GET'."));
        }

        [Test]
        public void Test6()
        {
            var actual = new ResponseBody
            {
                StatusCode = 200,
                IsSuccess = true,
                Numbers = new List<int> { 1, 2, 3 },
                Method = Method.GET,
                InnerMessage = new InnerMessage { Message = "Done" }
            };
            var expected = new ResponseBody
            {
                StatusCode = 200,
                IsSuccess = true,
                Numbers = new List<int> { 1, 2, 3 },
                Method = Method.GET,
                InnerMessage = new InnerMessage { Message = "Waiting" }
            };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(actual, Matches.DeeplyWith(expected)));

            Assert.That(ex.Message, Does.Contain("Property 'InnerMessage.Message' mismatch: Expected 'Waiting', but was 'Done'."));
        }
    }
}
