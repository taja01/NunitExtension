using NUnit.Framework.Constraints;

namespace DeepCompare.NUnitExtension
{
    /// <summary>
    /// Represents the result of applying a DeeplyEqualConstraint to an actual value.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="DeeplyEqualConstraintResult"/> class.
    /// </remarks>
    /// <param name="constraint">The constraint that was applied.</param>
    /// <param name="actualValue">The actual value to which the constraint was applied.</param>
    /// <param name="comparisonResult">The result of the deep equality comparison.</param>
    public class DeeplyEqualConstraintResult(IConstraint constraint, object? actualValue, List<(bool success, string propertyName, object? expectedValue, object? actualValue)> comparisonResult) : ConstraintResult(constraint, actualValue, comparisonResult.All(x => x.success))
    {
        public int ErrorCount => _comparisonResult.Count(x => !x.Success);

#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        private readonly List<(bool Success, string PropertyName, object ExpectedValue, object ActualValue)> _comparisonResult = comparisonResult;
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

        /// <summary>
        /// Writes the failure message for this result to the specified writer.
        /// </summary>
        /// <param name="writer">The writer to write the message to.</param>
        public override void WriteMessageTo(MessageWriter writer)
        {
            static object StringHelper(object? v) =>
                v switch
                {
                    null => "null",
                    string s when s.Length == 0 => "string.Empty",
                    _ => v
                };

            var errors = _comparisonResult.Where(x => !x.Success).ToList();
            if (!errors.Any()) return;

            writer.WriteLine($"Differences found: {errors.Count}. The details are as follows:");

            foreach (var result in errors)
            {
                var message = string.IsNullOrEmpty(result.PropertyName)
                    ? $"Mismatch: Expected '{StringHelper(result.ExpectedValue)}', but was '{StringHelper(result.ActualValue)}'."
                    : $"Property '{result.PropertyName}' mismatch: Expected '{StringHelper(result.ExpectedValue)}', but was '{StringHelper(result.ActualValue)}'.";
                writer.WriteLine(message);
            }
        }
    }
}
