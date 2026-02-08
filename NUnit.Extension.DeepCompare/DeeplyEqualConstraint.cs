using NUnit.Framework.Constraints;
using System.Collections;
using System.Reflection;

namespace DeepCompare.NUnitExtension
{
    /// <summary>
    /// A custom constraint class that checks if two objects are deeply equal
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="DeeplyEqualConstraint"/> class
    /// </remarks>
    /// <param name="expected">The expected object to compare with</param>
    /// <param name="options">Options to control comparison behavior</param>
    public class DeeplyEqualConstraint(object expected, DeepCompareOptions? options = null) : Constraint
    {
        private readonly object _expected = expected;
        private readonly DeepCompareOptions _options = options ?? new DeepCompareOptions();

        public override string Description => "Deeply equal objects";

        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            var result = DeepCompare(_expected, actual, string.Empty);
            return new DeeplyEqualConstraintResult(this, actual, result);
        }

        /// <summary>
        /// Fluent helper to configure options after creating the constraint.
        /// Allows: Matches.DeeplyWith(expected).WithOptions(o => o.Skip("Id"));
        /// Returns the same constraint for chaining and compatibility with NUnit.
        /// </summary>
        public DeeplyEqualConstraint WithOptions(Action<DeepCompareOptions> configure)
        {
            if (configure is null) return this;
            configure(_options);
            return this;
        }

        private List<(bool Success, string PropertyName, object? ExpectedValue, object? ActualValue)> DeepCompare(object? expected, object? actual, string parentPropertyName)
        {
            var differences = new List<(bool, string, object?, object?)>();

            // If both null -> equal
            if (expected == null && actual == null)
                return differences; // empty = no diffs

            // If only one is null -> difference
            if (expected == null || actual == null)
            {
                differences.Add((false, $"{parentPropertyName}".TrimEnd('.'), expected, actual));
                return differences;
            }

            var expectedType = expected.GetType();
            var actualType = actual.GetType();

            // Different types -> difference
            if (expectedType != actualType)
            {
                differences.Add((false, $"Different Type: {parentPropertyName}".TrimEnd('.'), $"{expectedType.Name}", $"{actualType.Name}"));
                return differences;
            }

            // Value types (including primitives, enums, structs) and strings
            if (expectedType.IsValueType || expected is string)
            {
                // DateTime / DateTimeOffset handling with tolerance
                if (IsDateTimeLike(expectedType))
                {
                    if (!CompareDateTimesWithTolerance(expected, actual, parentPropertyName, out var matched))
                    {
                        differences.Add((false, $"{parentPropertyName}".TrimEnd('.'), expected, actual));
                    }
                    return differences;
                }

                if (!Equals(expected, actual))
                {
                    differences.Add((false, $"{parentPropertyName}".TrimEnd('.'), expected, actual));
                }
                return differences;
            }

            // Collections
            if (expectedType.GetInterface(nameof(ICollection)) != null)
            {
                if (expected is ICollection expectedList && actual is ICollection actualList)
                {
                    var nestedResult = CompareLists(expectedList, actualList, $"{parentPropertyName}".TrimEnd('.') + ".");
                    if (nestedResult.Any(x => !x.Success))
                        differences.AddRange(nestedResult);
                    return differences;
                }
            }

            // Reference type: iterate properties
            var props = expectedType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                var fullName = string.IsNullOrEmpty(parentPropertyName) ? prop.Name : $"{parentPropertyName}{prop.Name}";
                // Skip check: match exact or suffix
                if (IsSkipped(fullName))
                    continue;

                var expectedValue = prop.GetValue(expected);
                var actualProp = actualType.GetProperty(prop.Name);
                object? actualValue = actualProp != null ? actualProp.GetValue(actual) : null;

                // both null -> continue
                if (expectedValue == null && actualValue == null)
                    continue;

                // one null -> difference
                if (expectedValue == null || actualValue == null)
                {
                    differences.Add((false, fullName, expectedValue, actualValue));
                    continue;
                }

                // If collection
                if (expectedValue is ICollection expectedColl && actualValue is ICollection actualColl)
                {
                    var nested = CompareLists(expectedColl, actualColl, $"{fullName}.");
                    if (nested.Any(x => !x.Success))
                        differences.AddRange(nested);
                    continue;
                }

                // Value types or string
                if (expectedValue.GetType().IsValueType || expectedValue is string)
                {
                    // DateTime handling
                    if (IsDateTimeLike(expectedValue.GetType()))
                    {
                        if (!CompareDateTimesWithTolerance(expectedValue, actualValue, fullName, out var matchedDT))
                        {
                            differences.Add((false, fullName, expectedValue, actualValue));
                        }
                        continue;
                    }

                    if (!Equals(expectedValue, actualValue))
                    {
                        differences.Add((false, fullName, expectedValue, actualValue));
                    }
                    continue;
                }

                // Complex object -> recurse
                var nestedResult = DeepCompare(expectedValue, actualValue, $"{fullName}.");
                if (nestedResult.Any(x => !x.Success))
                    differences.AddRange(nestedResult);
            }

            return differences;
        }

        private List<(bool Success, string PropertyName, object? ExpectedValue, object? ActualValue)> CompareLists(ICollection expectedCollection, ICollection actualCollection, string parentPropertyName)
        {
            var differences = new List<(bool, string, object?, object?)>();

            if (expectedCollection.Count != actualCollection.Count)
            {
                differences.Add((false, $"{parentPropertyName}Count".TrimEnd('.'), $"Count {expectedCollection.Count}", $"Count {actualCollection.Count}"));
                return differences;
            }

            var expectedEnumerator = expectedCollection.GetEnumerator();
            var actualEnumerator = actualCollection.GetEnumerator();
            var index = 0;

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

            return differences;
        }

        private bool IsSkipped(string fullPropertyName)
        {
            if (_options.SkippedProperties.Count == 0)
                return false;

            // exact or suffix match (case-insensitive already via HashSet)
            if (_options.SkippedProperties.Contains(fullPropertyName))
                return true;

            // suffix match: if any skipped entry matches end of fullPropertyName
            foreach (var skip in _options.SkippedProperties)
            {
                if (fullPropertyName.EndsWith(skip, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private bool IsDateTimeLike(Type t)
        {
            return t == typeof(DateTime) || t == typeof(DateTime?) || t == typeof(DateTimeOffset) || t == typeof(DateTimeOffset?);
        }

        private bool CompareDateTimesWithTolerance(object expected, object actual, string fullPropertyName, out bool matched)
        {
            matched = false;

            DateTimeOffset expectedDto;
            DateTimeOffset actualDto;

            try
            {
                if (expected is DateTime dtExp)
                    expectedDto = new DateTimeOffset(dtExp);
                else if (expected is DateTimeOffset dtoExp)
                    expectedDto = dtoExp;
                else
                {
                    matched = false;
                    return false;
                }

                if (actual is DateTime dtAct)
                    actualDto = new DateTimeOffset(dtAct);
                else if (actual is DateTimeOffset dtoAct)
                    actualDto = dtoAct;
                else
                {
                    matched = false;
                    return false;
                }
            }
            catch
            {
                matched = false;
                return false;
            }

            // Determine tolerance: per-property override first, then global
            TimeSpan? tolerance = null;
            if (_options.DateTimeTolerances.Count > 0)
            {
                // exact or suffix match
                if (_options.DateTimeTolerances.TryGetValue(fullPropertyName, out var tExact))
                    tolerance = tExact;
                else
                {
                    foreach (var kv in _options.DateTimeTolerances)
                    {
                        if (fullPropertyName.EndsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                        {
                            tolerance = kv.Value;
                            break;
                        }
                    }
                }
            }

            if (tolerance == null)
                tolerance = _options.GlobalDateTimeTolerance;

            if (tolerance == null)
            {
                matched = expectedDto.Equals(actualDto);
                return matched;
            }

            var diff = (expectedDto - actualDto).Duration();
            matched = diff <= tolerance.Value;
            return matched;
        }
    }
}
