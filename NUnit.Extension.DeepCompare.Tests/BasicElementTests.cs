using NUnit.Framework;

namespace DeepCompare.NUnitExtension.Tests
{

    [TestFixture]
    public class BasicElementTests
    {
        private const string StringOne = "123";
        private const string StringTwo = "321";
        private const object NullObject = null;
        private const int Integer = 123;

        [Test]
        public void StringStringTest()
        {
            Assert.That(StringOne, Matches.DeeplyWith(StringOne).Build());
        }

        [Test]
        public void StringWithDifferentStringTest()
        {
            var ex = Assert.Throws<AssertionException>(() => Assert.That(StringOne, Matches.DeeplyWith(StringTwo).Build()));

            Assert.That(ex.Message, Does.Contain("Differences found: 1. The details are as follows:"));
            Assert.That(ex.Message, Does.Contain("Mismatch: Expected '321', but was '123'."));
        }

        [Test]
        public void NullObjectAndStringTest()
        {
            var ex = Assert.Throws<AssertionException>(() => Assert.That(StringOne, Matches.DeeplyWith(NullObject).Build()));

            Assert.That(ex.Message, Does.Contain("Differences found: 1. The details are as follows:"));
            Assert.That(ex.Message, Does.Contain("Mismatch: Expected 'null', but was '123'"));
        }

        [Test]
        public void IntegerAndStringTest()
        {
            var ex = Assert.Throws<AssertionException>(() => Assert.That(StringOne, Matches.DeeplyWith(Integer).Build()));

            Assert.That(ex.Message, Does.Contain("Differences found: 1. The details are as follows:"));
            Assert.That(ex.Message, Does.Contain("Property 'Different Type: ' mismatch: Expected 'Int32', but was 'String'"));
        }

        [Test]
        public void NullObjectAndNullObjectTest()
        {
            Assert.That(NullObject, Matches.DeeplyWith(NullObject).Build());
        }

        [Test]
        public void IntegerAndNullObjectTest()
        {
            var ex = Assert.Throws<AssertionException>(() => Assert.That(NullObject, Matches.DeeplyWith(Integer).Build()));

            Assert.That(ex.Message, Does.Contain("Differences found: 1. The details are as follows:"));
            Assert.That(ex.Message, Does.Contain("Mismatch: Expected '123', but was 'null'"));
        }
    }
}