using NUnit.Framework.Constraints;
using System.Collections;

namespace DeepCompare.NUnitExtension
{
    /// <summary>
    /// A custom constraint class that checks if two objects are deeply equal
    /// </summary>
    public class DeeplyEqualConstraint : Constraint
    {
        // Declare a private field to store the expected object
        private readonly object _expected;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeeplyEqualConstraint"/> class
        /// </summary>
        /// <param name="expected">The expected object to compare with</param>
        public DeeplyEqualConstraint(object expected)
        {
            _expected = expected;
        }

        public override string Description => "Deeply equal objects";

        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            var result = DeepCompare(_expected, actual, string.Empty);
            return new DeeplyEqualConstraintResult(this, actual, result);
        }

        /// <summary>
        /// Compares two objects deeply and returns a tuple of results
        /// </summary>
        /// <param name="expected">The expected object to compare with</param>
        /// <param name="actual">The actual object to compare with</param>
        /// <param name="parentPropertyName">The name of the parent property that contains the objects</param>
        /// <returns>A tuple of four values: a boolean indicating the success of the comparison, a string indicating the name of the mismatched property, and two objects representing the expected and actual values of the mismatched property</returns>
        private List<(bool Success, string PropertyName, object ExpectedValue, object ActualValue)> DeepCompare(object expected, object actual, string parentPropertyName)
        {
            var differences = new List<(bool, string, object, object)>();
            // If both objects are null, they are equal, so return true and empty values
            if (expected == null && actual == null)
            {
                return differences;
            }

            // If one object is null and the other is not, they are not equal, so return false and the values
            if (expected == null || actual == null)
            {
                differences.Add((false, $"{parentPropertyName}", expected, actual));
                return differences;
            }

            // Get the types of the objects
            var expectedType = expected.GetType();
            var actualType = actual.GetType();

            // If the types are different, they are not equal, so return false and the type names
            if (expectedType != actualType)
            {
                differences.Add((false, $"Different Type: {parentPropertyName}", $"{expectedType.Name}", $"{actualType.Name}"));
                return differences;
            }

            // Get the properties of both objects using reflection
            var expectedProperties = expectedType.GetProperties();
            var actualProperties = actualType.GetProperties();

            // If the type is a value type, compare the objects using the Equals method
            if (expectedType.IsValueType)
            {
                if (!expected.Equals(actual))
                {
                    // If the objects are not equal, return false and the values
                    differences.Add((false, $"{parentPropertyName}", expected, actual));
                }
            }
            // If the type implements the ICollection interface, compare the objects as collections
            else if (expectedType.GetInterface("ICollection") != null)
            {
                // Cast the objects as collections
                if (expected is ICollection expectedList && actual is ICollection actualList)
                {
                    // Compare the collections using the CompareLists method and get the result
                    var nestedResult = CompareLists(expectedList, actualList, $"{parentPropertyName}{expectedProperties}.");

                    // If the comparison failed, return the result
                    if (nestedResult.Any(x => !x.Success))
                    {
                        differences.AddRange(nestedResult);
                    }
                    return differences;
                }
            }
            // If the type is a reference type, compare the objects recursively by their properties
            else
            {
                // Loop through each property of both objects
                foreach (var expectedProperty in expectedProperties)
                {
                    // Get the corresponding property of the actual object by name
                    var actualProperty = actualType.GetProperty(expectedProperty.Name);

                    // If the actual property is null, it means the actual object does not have that property
                    //if (actualProperty == null)
                    //{
                    //    // Return false and the details of the mismatched property and values
                    //    return (false, $"{parentPropertyName}{expectedProperty.Name}", expectedProperty.GetValue(expected), null);
                    //}

                    // Get the values of the expected and actual properties
                    var expectedValue = expectedProperty.ReflectedType.Name.Equals(nameof(String)) ? expected : expectedProperty.GetValue(expected);
                    var actualValue = actualProperty.ReflectedType.Name.Equals(nameof(String)) ? actual : actualProperty.GetValue(actual);

                    // If both values are null, they are equal, so continue to the next property
                    if (expectedValue == null && actualValue == null)
                    {
                        continue;
                    }
                    // If one value is null and the other is not, they are not equal, so return false and the details of the mismatched property and values
                    else if (expectedValue == null || actualValue == null)
                    {
                        differences.Add((false, $"{parentPropertyName}{expectedProperty.Name}", expectedValue, actualValue));
                        continue;
                    }

                    // If both values are collections, compare them using the CompareLists method
                    if (expectedValue is ICollection expectedList && actualValue is ICollection actualList)
                    {
                        // Get the result of the comparison, which includes a success flag and the details of the first mismatched element, if any
                        var nestedResult = CompareLists(expectedList, actualList, $"{parentPropertyName}{expectedProperty.Name}.");

                        // If the comparison failed, return the result
                        if (nestedResult.Any(x => !x.Success))
                        {
                            differences.AddRange(nestedResult);
                        }
                    }
                    // If both values are value types or strings, compare them using the Equals method
                    else if (expectedValue.GetType().IsValueType || expectedValue is string)
                    {
                        if (!Equals(expectedValue, actualValue))
                        {
                            differences.Add((false, $"{parentPropertyName}{expectedProperty.Name}", expectedValue, actualValue));
                        }
                    }
                    // If the values are not equal, return false and the details of the mismatched property and values
                    else
                    {
                        var nestedResult = DeepCompare(expectedValue, actualValue, $"{parentPropertyName}{expectedProperty.Name}.");

                        // If the comparison failed, return the result
                        if (nestedResult.Any(x => !x.Success))
                        {
                            differences.AddRange(nestedResult);
                        }
                    }
                }
            }

            // If no mismatch was found, return true and empty value
            return differences;
        }

        /// <summary>
        /// Compares two collections and returns a tuple of results
        /// </summary>
        /// <param name="expectedCollection">The expected collection to compare with</param>
        /// <param name="actualCollection">The actual collection to compare with</param>
        /// <param name="parentPropertyName">The name of the parent property that contains the collections</param>
        /// <returns>A tuple of four values: a boolean indicating the success of the comparison, a string indicating the name of the mismatched element, and two objects representing the expected and actual values of the mismatched element</returns>
        private List<(bool Success, string PropertyName, object ExpectedValue, object ActualValue)> CompareLists(ICollection expectedCollection, ICollection actualCollection, string parentPropertyName)
        {
            var differences = new List<(bool, string, object, object)>();

            // If the collections have different counts, they are not equal, so return false and the counts
            if (expectedCollection.Count != actualCollection.Count)
            {
                differences.Add((false, $"{parentPropertyName}Count", $"Count {expectedCollection.Count}", $"Count {actualCollection.Count}"));
                return differences;
            }

            // Get the enumerators of the collections
            var expectedEnumerator = expectedCollection.GetEnumerator();
            var actualEnumerator = actualCollection.GetEnumerator();

            // Initialize an index variable to keep track of the current element
            var index = 0;

            // Loop through the collections until one of them reaches the end
            while (expectedEnumerator.MoveNext() && actualEnumerator.MoveNext())
            {
                var expectedElement = expectedEnumerator.Current;
                var actualElement = actualEnumerator.Current;
                var nestedResult = DeepCompare(expectedElement, actualElement, $"{parentPropertyName}[{index}].");

                if (nestedResult.Any(x => !x.Success))
                {
                    differences.AddRange(nestedResult);
                }

                index++;
            }

            if (differences.Count == 0)
            {
                differences.Add((true, string.Empty, null, null));
            }

            // If no mismatch was found, return true and empty value
            return differences;
        }
    }
}
