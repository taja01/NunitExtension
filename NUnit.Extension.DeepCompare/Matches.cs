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
        /// Optional explicit Build() - returns the constraint (no-op).
        /// </summary>
        public DeeplyEqualConstraint Build() => this;
    }
}
