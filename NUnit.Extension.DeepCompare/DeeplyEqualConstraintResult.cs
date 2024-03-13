using NUnit.Framework.Constraints;

namespace DeepCompare.NUnitExtension
{
    /// <summary>
    /// Represents the result of applying a DeeplyEqualConstraint to an actual value.
    /// </summary>
    public class DeeplyEqualConstraintResult : ConstraintResult
    {
        public int ErrorCount => _comparisonResult.Count(x => !x.Success);

        private readonly List<(bool Success, string PropertyName, object ExpectedValue, object ActualValue)> _comparisonResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeeplyEqualConstraintResult"/> class.
        /// </summary>
        /// <param name="constraint">The constraint that was applied.</param>
        /// <param name="actualValue">The actual value to which the constraint was applied.</param>
        /// <param name="comparisonResult">The result of the deep equality comparison.</param>
        public DeeplyEqualConstraintResult(IConstraint constraint, object actualValue, List<(bool success, string propertyName, object expectedValue, object actualValue)> comparisonResult)
            : base(constraint, actualValue, comparisonResult.All(x => x.success))

        {
            _comparisonResult = comparisonResult;
        }

        /// <summary>
        /// Writes the failure message for this result to the specified writer.
        /// </summary>
        /// <param name="writer">The writer to write the message to.</param>
        public override void WriteMessageTo(MessageWriter writer)
        {
            // Define a helper method to convert any object to a string representation
            object StringHelper(object expectedValue)
            {
                // Use a switch expression to handle different cases
                return expectedValue switch
                {
                    // If the object is null, return "null"
                    null => "null",
                    // If the object is a string and is empty, return "string.Empty"
                    string str when string.IsNullOrEmpty(str) => nameof(string.Empty),
                    // Otherwise, return the object itself
                    _ => expectedValue,
                };
            }

            // If the comparison result is not successful, write a message to the writer
            if (_comparisonResult.All(x => x.Success))
            {
                return;
            }

            writer.WriteLine($"Differences found: {_comparisonResult.Count()}. The details are as follows:");

            foreach (var result in _comparisonResult.Where(x => !x.Success))
            {
                // Use the ternary operator to choose between two messages
                string message = string.IsNullOrEmpty(result.PropertyName)
                    ? $"Mismatch: Expected '{result.ExpectedValue}', but was '{result.ActualValue}'."
                    : $"Property '{result.PropertyName}' mismatch: Expected '{StringHelper(result.ExpectedValue)}', but was '{StringHelper(result.ActualValue)}'.";

                // Write the message to the writer
                writer.WriteLine(message);
            }
        }
    }
}
