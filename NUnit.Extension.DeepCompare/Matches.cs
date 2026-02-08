namespace DeepCompare.NUnitExtension
{
    public class Matches : NUnit.Framework.Is
    {
        public static DeeplyWithBuilder DeeplyWith(object expected, Action<DeepCompareOptions>? configure = null)
        {
            var options = new DeepCompareOptions();
            configure?.Invoke(options);
            return new DeeplyWithBuilder(expected, options);
        }

        /// <summary>
        /// Small fluent builder returned from Matches.DeeplyWith(...)
        /// Implicitly converts to the underlying Constraint so existing usages
        /// like Assert.That(actual, Matches.DeeplyWith(expected)) keep working.
        /// </summary>
        public sealed class DeeplyWithBuilder
        {
            private readonly DeeplyEqualConstraint _constraint;

            internal DeeplyWithBuilder(object expected, DeepCompareOptions options)
            {
                _constraint = new DeeplyEqualConstraint(expected, options);
            }

            public DeeplyWithBuilder WithOptions(Action<DeepCompareOptions> configure)
            {
                if (configure is null) return this;
                _constraint.WithOptions(configure);
                return this;
            }

            public DeeplyWithBuilder Skip(string propertyPath)
            {
                _constraint.WithOptions(o => o.Skip(propertyPath));
                return this;
            }

            public DeeplyWithBuilder WithGlobalDateTimeTolerance(TimeSpan tolerance)
            {
                _constraint.WithOptions(o => o.WithGlobalDateTimeTolerance(tolerance));
                return this;
            }

            public DeeplyWithBuilder WithDateTimeTolerance(string propertyPath, TimeSpan tolerance)
            {
                _constraint.WithOptions(o => o.WithDateTimeTolerance(propertyPath, tolerance));
                return this;
            }

            public DeeplyEqualConstraint Build() => _constraint;

            // Allow use directly in Assert.That(...) and other APIs expecting a Constraint
            public static implicit operator DeeplyEqualConstraint(DeeplyWithBuilder b) => b._constraint;
            public static implicit operator NUnit.Framework.Constraints.Constraint(DeeplyWithBuilder b) => b._constraint;
        }
    }
}
