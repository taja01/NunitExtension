using NUnit.Framework.Constraints;

namespace NUnit.Extension.DeepCompare
{
    /// <summary>
    /// Represents the result of applying a DeeplyEqualConstraint to an actual value.
    /// </summary>
    public class DeeplyEqualConstraintResult : ConstraintResult
    {
        private readonly (bool Success, string PropertyName, object ExpectedValue, object ActualValue) _comparisonResult;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeeplyEqualConstraintResult"/> class.
        /// </summary>
        /// <param name="constraint">The constraint that was applied.</param>
        /// <param name="actualValue">The actual value to which the constraint was applied.</param>
        /// <param name="comparisonResult">The result of the deep equality comparison.</param>
        public DeeplyEqualConstraintResult(IConstraint constraint, object actualValue, (bool success, string propertyName, object expectedValue, object actualValue) comparisonResult)
            : base(constraint, actualValue, comparisonResult.success)
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
            if (!_comparisonResult.Success)
            {
                // Use the ternary operator to choose between two messages
                string message = string.IsNullOrEmpty(_comparisonResult.PropertyName)
                    ? $"Mismatch: Expected '{_comparisonResult.ExpectedValue}', but was '{_comparisonResult.ActualValue}'."
                    : $"Property '{_comparisonResult.PropertyName}' mismatch: Expected '{StringHelper(_comparisonResult.ExpectedValue)}', but was '{StringHelper(_comparisonResult.ActualValue)}'.";

                // Write the message to the writer
                writer.WriteLine(message);
            }
        }
    }
}
