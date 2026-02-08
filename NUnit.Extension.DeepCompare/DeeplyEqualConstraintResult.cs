using NUnit.Framework.Constraints;

namespace DeepCompare.NUnitExtension
{
    /// <summary>
    /// Represents the result of applying a <see cref="DeeplyEqualConstraint"/> to an actual value.
    /// Holds the comparison tuple entries and is responsible for writing user-friendly failure messages.
    /// </summary>
    /// <param name="constraint">The constraint that was applied.</param>
    /// <param name="actualValue">The actual value to which the constraint was applied.</param>
    /// <param name="comparisonResult">The per-property comparison result tuples.</param>
    public class DeeplyEqualConstraintResult(IConstraint constraint, object? actualValue, List<(bool success, string propertyName, object? expectedValue, object? actualValue)> comparisonResult)
        : ConstraintResult(constraint, actualValue, comparisonResult.All(x => x.success))
    {
        private readonly IConstraint Constraint = constraint;

        /// <summary>
        /// Number of differences (entries where Success == false).
        /// </summary>
        public int ErrorCount => _comparisonResult.Count(x => !x.Success);



#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
        private readonly List<(bool Success, string PropertyName, object ExpectedValue, object ActualValue)> _comparisonResult = comparisonResult;
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.

        /// <summary>
        /// Writes detailed failure information to the supplied <see cref="MessageWriter"/>.
        /// </summary>
        /// <param name="writer">The writer used by NUnit to display assertion failures.</param>
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
            if (errors.Count == 0)
            {
                return;
            }

            var limit = ((DeeplyEqualConstraint)Constraint).MaxDifferences;
            if (limit == errors.Count)
            {
                writer.WriteLine($"Maximum limit of {limit} reached.");
            }

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
