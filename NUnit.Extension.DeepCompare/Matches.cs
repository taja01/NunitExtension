using NUnit.Framework.Constraints;

namespace DeepCompare.NUnitExtension
{
    public class Matches : NUnit.Framework.Is
    {
        public static DeeplyEqualConstraint DeeplyWith(object expected, System.Action<DeepCompareOptions>? configure = null)
        {
            var options = new DeepCompareOptions();
            configure?.Invoke(options);
            return new DeeplyEqualConstraint(expected, options);
        }
    }

    public partial class DeeplyEqualConstraint
    {
        /// <summary>
        /// Fluent helper to skip a property (convenience wrapper around Options).
        /// Usage: Matches.DeeplyWith(expected).Skip("Inner.Message")
        /// </summary>
        public DeeplyEqualConstraint Skip(string propertyPath)
        {
            if (!string.IsNullOrEmpty(propertyPath))
                _options.Skip(propertyPath);
            return this;
        }

        /// <summary>
        /// Fluent helper to set a global DateTime tolerance.
        /// Usage: Matches.DeeplyWith(expected).WithGlobalDateTimeTolerance(TimeSpan.FromSeconds(1))
        /// </summary>
        public DeeplyEqualConstraint WithGlobalDateTimeTolerance(TimeSpan tolerance)
        {
            _options.WithGlobalDateTimeTolerance(tolerance);
            return this;
        }

        /// <summary>
        /// Fluent helper to set a per-property DateTime tolerance.
        /// </summary>
        public DeeplyEqualConstraint WithDateTimeTolerance(string propertyPath, TimeSpan tolerance)
        {
            if (!string.IsNullOrEmpty(propertyPath))
                _options.WithDateTimeTolerance(propertyPath, tolerance);
            return this;
        }

        /// <summary>
        /// Applies the deep-equality constraint to the provided actual value.
        /// </summary>
        /// <typeparam name="TActual">The compile-time type of the actual value.</typeparam>
        /// <param name="actual">The actual value to compare against the expected object supplied to the constraint.</param>
        /// <returns>
        /// A <see cref="ConstraintResult"/> that contains success/failure and diagnostic details produced
        /// by the comparison.
        /// </returns>
        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            // Create a per-assertion visited set of (expected, actual) reference pairs to detect cycles
            var visited = new HashSet<(object? expected, object? actual)>(PairReferenceComparer.Instance);

            var result = DeepCompare(_expected, actual, string.Empty, visited);
            return new DeeplyEqualConstraintResult(this, actual, result);
        }

        /// <summary>
        /// Configure this constraint via a callback that modifies <see cref="DeepCompareOptions"/>.
        /// </summary>
        /// <param name="configure">Callback to configure comparison options (skip rules, tolerances, max diffs).</param>
        /// <returns>This constraint instance for fluent chaining.</returns>
        public DeeplyEqualConstraint WithOptions(Action<DeepCompareOptions> configure)
        {
            if (configure is null) return this;
            configure(_options);
            return this;
        }

        /// <summary>
        /// Optional explicit Build() - returns the constraint (no-op).
        /// </summary>
        public DeeplyEqualConstraint Build() => this;
    }
}
