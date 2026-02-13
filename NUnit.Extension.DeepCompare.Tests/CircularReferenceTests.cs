using NUnit.Framework;

namespace DeepCompare.NUnitExtension.Tests
{
    [TestFixture]
    public class CircularReferenceTests
    {
        [Test]
        public void SimpleCircularReference_IdenticalGraphs_Passes()
        {
            // Arrange
            var node1 = new Node { Value = 1 };
            var node2 = new Node { Value = 2 };
            node1.Next = node2;
            node2.Next = node1; // Circular reference

            var otherNode1 = new Node { Value = 1 };
            var otherNode2 = new Node { Value = 2 };
            otherNode1.Next = otherNode2;
            otherNode2.Next = otherNode1; // Circular reference

            // Act & Assert
            Assert.DoesNotThrow(() => Assert.That(node1, Matches.DeeplyWith(otherNode1)));
        }

        [Test]
        public void SimpleCircularReference_DifferentValues_ReportsDifference()
        {
            // Arrange
            var node1 = new Node { Value = 1 };
            var node2 = new Node { Value = 2 };
            node1.Next = node2;
            node2.Next = node1; // Circular reference

            var otherNode1 = new Node { Value = 1 };
            var otherNode2 = new Node { Value = 99 }; // Different value
            otherNode1.Next = otherNode2;
            otherNode2.Next = otherNode1; // Circular reference

            // Act & Assert
            var ex = Assert.Throws<AssertionException>(() => Assert.That(node1, Matches.DeeplyWith(otherNode1)));
            Assert.That(ex.Message, Does.Contain("Next.Value"));
            Assert.That(ex.Message, Does.Contain("Expected '99', but was '2'"));
        }

        [Test]
        public void SelfReferencing_Node_Passes()
        {
            // Arrange
            var node1 = new Node { Value = 42 };
            node1.Next = node1; // Points to itself

            var node2 = new Node { Value = 42 };
            node2.Next = node2; // Points to itself

            // Act & Assert
            Assert.DoesNotThrow(() => Assert.That(node1, Matches.DeeplyWith(node2)));
        }

        [Test]
        public void SelfReferencing_Node_DifferentValue_ReportsDifference()
        {
            // Arrange
            var node1 = new Node { Value = 42 };
            node1.Next = node1; // Points to itself

            var node2 = new Node { Value = 99 };
            node2.Next = node2; // Points to itself

            // Act & Assert
            var ex = Assert.Throws<AssertionException>(() => Assert.That(node1, Matches.DeeplyWith(node2)));
            Assert.That(ex.Message, Does.Contain("Value"));
            Assert.That(ex.Message, Does.Contain("Expected '99', but was '42'"));
        }

        [Test]
        public void ComplexCircularGraph_ThreeNodes_IdenticalGraphs_Passes()
        {
            // Arrange
            var a1 = new Node { Value = 1 };
            var b1 = new Node { Value = 2 };
            var c1 = new Node { Value = 3 };
            a1.Next = b1;
            b1.Next = c1;
            c1.Next = a1; // Back to start - circular

            var a2 = new Node { Value = 1 };
            var b2 = new Node { Value = 2 };
            var c2 = new Node { Value = 3 };
            a2.Next = b2;
            b2.Next = c2;
            c2.Next = a2; // Back to start - circular

            // Act & Assert
            Assert.DoesNotThrow(() => Assert.That(a1, Matches.DeeplyWith(a2)));
        }

        [Test]
        public void ComplexCircularGraph_DifferentMiddleValue_ReportsDifference()
        {
            // Arrange
            var a1 = new Node { Value = 1 };
            var b1 = new Node { Value = 2 };
            var c1 = new Node { Value = 3 };
            a1.Next = b1;
            b1.Next = c1;
            c1.Next = a1; // Circular

            var a2 = new Node { Value = 1 };
            var b2 = new Node { Value = 999 }; // Different!
            var c2 = new Node { Value = 3 };
            a2.Next = b2;
            b2.Next = c2;
            c2.Next = a2; // Circular

            // Act & Assert
            var ex = Assert.Throws<AssertionException>(() => Assert.That(a1, Matches.DeeplyWith(a2)));
            Assert.That(ex.Message, Does.Contain("Next.Value"));
            Assert.That(ex.Message, Does.Contain("Expected '999', but was '2'"));
        }

        [Test]
        public void ParentChild_BidirectionalReference_Passes()
        {
            // Arrange
            var parent1 = new Parent { Name = "Dad" };
            var child1 = new Child { Name = "Son", ParentRef = parent1 };
            parent1.ChildRef = child1;

            var parent2 = new Parent { Name = "Dad" };
            var child2 = new Child { Name = "Son", ParentRef = parent2 };
            parent2.ChildRef = child2;

            // Act & Assert
            Assert.DoesNotThrow(() => Assert.That(parent1, Matches.DeeplyWith(parent2)));
        }

        [Test]
        public void ParentChild_BidirectionalReference_DifferentChildName_ReportsDifference()
        {
            // Arrange
            var parent1 = new Parent { Name = "Dad" };
            var child1 = new Child { Name = "Son", ParentRef = parent1 };
            parent1.ChildRef = child1;

            var parent2 = new Parent { Name = "Dad" };
            var child2 = new Child { Name = "Daughter", ParentRef = parent2 }; // Different!
            parent2.ChildRef = child2;

            // Act & Assert
            var ex = Assert.Throws<AssertionException>(() => Assert.That(parent1, Matches.DeeplyWith(parent2)));
            Assert.That(ex.Message, Does.Contain("ChildRef.Name"));
            Assert.That(ex.Message, Does.Contain("Expected 'Daughter', but was 'Son'"));
        }

        [Test]
        public void DoublyLinkedList_WithCycle_Passes()
        {
            // Arrange
            var node1 = new DoublyLinkedNode { Value = 1 };
            var node2 = new DoublyLinkedNode { Value = 2 };
            var node3 = new DoublyLinkedNode { Value = 3 };
            
            node1.Next = node2;
            node2.Previous = node1;
            node2.Next = node3;
            node3.Previous = node2;
            node3.Next = node1; // Cycle back
            node1.Previous = node3;

            var other1 = new DoublyLinkedNode { Value = 1 };
            var other2 = new DoublyLinkedNode { Value = 2 };
            var other3 = new DoublyLinkedNode { Value = 3 };
            
            other1.Next = other2;
            other2.Previous = other1;
            other2.Next = other3;
            other3.Previous = other2;
            other3.Next = other1; // Cycle back
            other1.Previous = other3;

            // Act & Assert
            Assert.DoesNotThrow(() => Assert.That(node1, Matches.DeeplyWith(other1)));
        }

        [Test]
        public void TreeWithMultipleReferencesToSameNode_Passes()
        {
            // Arrange - A tree where multiple nodes reference the same shared node
            var shared1 = new Node { Value = 999 };
            var left1 = new Node { Value = 1, Next = shared1 };
            var right1 = new Node { Value = 2, Next = shared1 };
            var root1 = new TreeNode { Value = 0, Left = left1, Right = right1 };

            var shared2 = new Node { Value = 999 };
            var left2 = new Node { Value = 1, Next = shared2 };
            var right2 = new Node { Value = 2, Next = shared2 };
            var root2 = new TreeNode { Value = 0, Left = left2, Right = right2 };

            // Act & Assert
            Assert.DoesNotThrow(() => Assert.That(root1, Matches.DeeplyWith(root2)));
        }

        [Test]
        public void CircularReferenceInCollection_Passes()
        {
            // Arrange
            var node1 = new Node { Value = 1 };
            var node2 = new Node { Value = 2 };
            node1.Next = node2;
            node2.Next = node1;

            var container1 = new Container { Nodes = new List<Node> { node1, node2 } };

            var otherNode1 = new Node { Value = 1 };
            var otherNode2 = new Node { Value = 2 };
            otherNode1.Next = otherNode2;
            otherNode2.Next = otherNode1;

            var container2 = new Container { Nodes = new List<Node> { otherNode1, otherNode2 } };

            // Act & Assert
            Assert.DoesNotThrow(() => Assert.That(container1, Matches.DeeplyWith(container2)));
        }

        [Test]
        public void MixedGraph_CycleAndNonCycle_ReportsDifferencesCorrectly()
        {
            // Arrange - Complex graph with both circular and non-circular parts
            var cycle1 = new Node { Value = 10 };
            var cycle2 = new Node { Value = 20 };
            cycle1.Next = cycle2;
            cycle2.Next = cycle1;

            var linear1 = new Node { Value = 100 };
            var linear2 = new Node { Value = 200, Next = linear1 };

            var graph1 = new Graph { CyclicPart = cycle1, LinearPart = linear2 };

            var otherCycle1 = new Node { Value = 10 };
            var otherCycle2 = new Node { Value = 20 };
            otherCycle1.Next = otherCycle2;
            otherCycle2.Next = otherCycle1;

            var otherLinear1 = new Node { Value = 999 }; // Different value!
            var otherLinear2 = new Node { Value = 200, Next = otherLinear1 };

            var graph2 = new Graph { CyclicPart = otherCycle1, LinearPart = otherLinear2 };

            // Act & Assert
            var ex = Assert.Throws<AssertionException>(() => Assert.That(graph1, Matches.DeeplyWith(graph2)));
            Assert.That(ex.Message, Does.Contain("LinearPart.Next.Value"));
            Assert.That(ex.Message, Does.Contain("Expected '999', but was '100'"));
        }

        // Helper classes for circular reference testing
        private class Node
        {
            public int Value { get; set; }
            public Node? Next { get; set; }
        }

        private class DoublyLinkedNode
        {
            public int Value { get; set; }
            public DoublyLinkedNode? Next { get; set; }
            public DoublyLinkedNode? Previous { get; set; }
        }

        private class Parent
        {
            public string? Name { get; set; }
            public Child? ChildRef { get; set; }
        }

        private class Child
        {
            public string? Name { get; set; }
            public Parent? ParentRef { get; set; }
        }

        private class TreeNode
        {
            public int Value { get; set; }
            public Node? Left { get; set; }
            public Node? Right { get; set; }
        }

        private class Container
        {
            public List<Node>? Nodes { get; set; }
        }

        private class Graph
        {
            public Node? CyclicPart { get; set; }
            public Node? LinearPart { get; set; }
        }
    }
}