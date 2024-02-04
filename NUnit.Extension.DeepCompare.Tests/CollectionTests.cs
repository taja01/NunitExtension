using NUnit.Framework;

namespace NUnit.Extension.DeepCompare.Tests
{
    [TestFixture]
    public class CollectionTests
    {
        [Test]
        public void DifferentCollectionTest()
        {
            var arr = new[] { new object() };
            var list = new List<object>();

            var ex = Assert.Throws<AssertionException>(() => Assert.That(arr, Is.DeeplyEqualTo(list)));

            Assert.That(ex.Message, Does.Contain("Property 'Different Type: ' mismatch: Expected 'List`1', but was 'Object[]'."));
        }

        [Test]
        public void StringCollectionTest()
        {
            var expectedList = new List<string?> { "1", "2", null };
            var actualList = new List<string?> { "1", "2", null };

            Assert.That(expectedList, Is.DeeplyEqualTo(actualList));
        }

        [Test]
        public void StringCollectionTest2()
        {
            var expectedList = new List<string> { "1", "2" };
            var actualList = new List<string> { "1", "2" };

            Assert.That(expectedList, Is.DeeplyEqualTo(actualList));
        }

        [Test]
        public void StringCollectionTest3()
        {
            var expectedList = new List<string?> { "1", "2", null };
            var actualList = new List<string?> { "1", "2", "" };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(expectedList, Is.DeeplyEqualTo(actualList)));

            Assert.That(ex.Message, Does.Contain("Property 'System.Reflection.PropertyInfo[].[2].' mismatch: Expected 'Empty', but was 'null'"));
        }

        [Test]
        public void StringCollectionTest4()
        {
            var expectedList = new List<string?> { "1", "2", "" };
            var actualList = new List<string?> { "1", "2", null };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(expectedList, Is.DeeplyEqualTo(actualList)));

            Assert.That(ex.Message, Does.Contain("Property 'System.Reflection.PropertyInfo[].[2].' mismatch: Expected 'null', but was 'Empty'"));
        }

        [Test]
        public void StringCollectionDDifferentLenghtTest()
        {
            var expectedList = new List<string?> { "1", "2", null, "4" };
            var actualList = new List<string?> { "1", "2", null };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(expectedList, Is.DeeplyEqualTo(actualList)));

            Assert.That(ex.Message, Does.Contain("Property 'System.Reflection.PropertyInfo[].Count' mismatch: Expected 'Count 3', but was 'Count 4'."));
        }

        [Test]
        public void IntCollectionTest()
        {
            var expectedList = new List<int?> { 1, 2, 3, null };
            var actualList = new List<int?> { 1, 2, 3, null };

            Assert.That(expectedList, Is.DeeplyEqualTo(actualList));
        }

        [Test]
        public void IntCollectionTest2()
        {
            var expectedList = new List<int?> { 1, 2, 3, null };
            var actualList = new List<int?> { 1, 2, 3, 4 };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(expectedList, Is.DeeplyEqualTo(actualList)));

            Assert.That(ex.Message, Does.Contain("Property 'System.Reflection.PropertyInfo[].[3].' mismatch: Expected '4', but was 'null'."));
        }

        [Test]
        public void IntCollectionTest3()
        {
            var expectedList = new List<int?> { 1, 2, 3, 4 };
            var actualList = new List<int?> { 1, 2, 3, null };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(expectedList, Is.DeeplyEqualTo(actualList)));

            Assert.That(ex.Message, Does.Contain("Property 'System.Reflection.PropertyInfo[].[3].' mismatch: Expected 'null', but was '4'."));
        }

        [Test]
        public void CharCollectionTest()
        {
            var expectedList = new List<char?> { 'a', '1', ' ' };
            var actualList = new List<char?> { 'a', '1', ' ' };

            Assert.That(expectedList, Is.DeeplyEqualTo(actualList));
        }

        [Test]
        public void CharCollectionTest2()
        {
            var expectedList = new List<char?> { 'a', '1', ' ', '+' };
            var actualList = new List<char?> { 'a', '1', ' ', '-' };

            var ex = Assert.Throws<AssertionException>(() => Assert.That(expectedList, Is.DeeplyEqualTo(actualList)));

            Assert.That(ex.Message, Does.Contain("Property 'System.Reflection.PropertyInfo[].[3].' mismatch: Expected '-', but was '+'."));
        }
    }
}
