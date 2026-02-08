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
    public partial class DeeplyEqualConstraint(object expected, DeepCompareOptions? options = null) : Constraint
    {
        private readonly object _expected = expected;
        private readonly DeepCompareOptions _options = options ?? new DeepCompareOptions();

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, PropertyInfo[]> _propsCache = new();

        private static PropertyInfo[] GetPropertiesCached(Type t) =>
            _propsCache.GetOrAdd(t, _ => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        public override string Description => "Deeply equal objects";

        public override ConstraintResult ApplyTo<TActual>(TActual actual)
        {
            // Create a per-assertion visited set of (expected, actual) reference pairs to detect cycles
            var visited = new HashSet<(object? expected, object? actual)>(PairReferenceComparer.Instance);

            var result = DeepCompare(_expected, actual, string.Empty, visited);
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

        private List<(bool Success, string PropertyName, object? ExpectedValue, object? ActualValue)> DeepCompare(
            object? expected,
            object? actual,
            string parentPropertyName,
            HashSet<(object? expected, object? actual)> visited)
        {
            var differences = new List<(bool, string, object?, object?)>();

            // If both null -> equal
            if (expected == null && actual == null)
                return differences; // empty = no diffs

            // If only one is null -> difference
            if (expected == null || actual == null)
            {
                differences.Add((false, parentPropertyName, expected, actual));
                return differences;
            }

            var expectedType = expected.GetType();
            var actualType = actual.GetType();

            // Different types -> normally difference, but allow comparing array/list-like collections
            if (expectedType != actualType)
            {
                if (IsCollectionType(expectedType) && IsCollectionType(actualType))
                {
                    var expectedElem = GetElementType(expectedType);
                    var actualElem = GetElementType(actualType);

                    // If element types are compatible, fall through to collection comparison.
                    if (expectedElem != null && actualElem != null &&
                        (expectedElem == actualElem ||
                         expectedElem.IsAssignableFrom(actualElem) ||
                         actualElem.IsAssignableFrom(expectedElem)))
                    {
                        // treat as comparable collections
                    }
                    else
                    {
                        differences.Add((false, $"Different Type: {parentPropertyName}".TrimStart('.'), $"{expectedType.Name}", $"{actualType.Name}"));
                        return differences;
                    }
                }
                else
                {
                    differences.Add((false, $"Different Type: {parentPropertyName}".TrimStart('.'), $"{expectedType.Name}", $"{actualType.Name}"));
                    return differences;
                }
            }

            // For reference types (not value types and not string) detect cycles using the visited pair set.
            var isReferenceType = !expectedType.IsValueType && !(expected is string);
            if (isReferenceType)
            {
                var pair = (expected, actual);
                if (visited.Contains(pair))
                {
                    // We've already compared this pair on this top-level comparison.
                    // Treat revisited pair as equal to avoid infinite recursion.
                    return differences;
                }

                // Track this pair for the duration of the top-level comparison.
                visited.Add(pair);
            }

            // Value types (including primitives, enums, structs) and strings
            if (expectedType.IsValueType || expected is string)
            {
                // DateTime / DateTimeOffset handling with tolerance
                if (IsDateTimeLike(expectedType))
                {
                    if (!CompareDateTimesWithTolerance(expected, actual, parentPropertyName, out var matched))
                    {
                        differences.Add((false, parentPropertyName, expected, actual));
                    }
                    return differences;
                }

                if (!Equals(expected, actual))
                {
                    differences.Add((false, parentPropertyName, expected, actual));
                }

                return differences;
            }

            // Collections (ICollection) - prefer IList for index-based comparison
            if (expectedType.GetInterface(nameof(ICollection)) != null)
            {
                if (expected is ICollection expectedList && actual is ICollection actualList)
                {
                    var nestedResult = CompareLists(expectedList, actualList, parentPropertyName, visited);
                    if (nestedResult.Any(x => !x.Success))
                        differences.AddRange(nestedResult);
                    return differences;
                }
            }

            // Reference type: iterate properties
            var props = GetPropertiesCached(expectedType);
            foreach (var prop in props)
            {
                var fullName = JoinPath(parentPropertyName, prop.Name);
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
                    var nested = CompareLists(expectedColl, actualColl, fullName, visited);
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

                // Complex object -> recurse, pass visited set to detect cycles
                var nestedResult = DeepCompare(expectedValue, actualValue, fullName, visited);
                if (nestedResult.Any(x => !x.Success))
                    differences.AddRange(nestedResult);
            }

            return differences;
        }

        private List<(bool Success, string PropertyName, object? ExpectedValue, object? ActualValue)> CompareLists(
            ICollection expectedCollection,
            ICollection actualCollection,
            string parentPropertyName,
            HashSet<(object? expected, object? actual)> visited)
        {
            var differences = new List<(bool, string, object?, object?)>();

            // Normalize parent path (do not trim leading/trailing bracket segments)
            parentPropertyName = parentPropertyName ?? string.Empty;

            // Track collection pair to avoid infinite recursion but do NOT short-circuit comparison
            if (expectedCollection is object && actualCollection is object)
            {
                var collectionPair = (expectedCollection as object, actualCollection as object);
                if (!visited.Contains(collectionPair))
                    visited.Add(collectionPair);
            }

            if (expectedCollection.Count != actualCollection.Count)
            {
                differences.Add((false, JoinPath(parentPropertyName, "Count"), $"Count {expectedCollection.Count}", $"Count {actualCollection.Count}"));
                return differences;
            }

            // Prefer IList for stable index access
            if (expectedCollection is IList expectedList && actualCollection is IList actualList)
            {
                for (var i = 0; i < expectedList.Count; i++)
                {
                    var expectedElement = expectedList[i];
                    var actualElement = actualList[i];

                    var elementPath = JoinPath(parentPropertyName, $"[{i}]");

                    // explicit null-vs-value check to ensure differences for nullable elements are reported
                    if (expectedElement == null && actualElement == null)
                    {
                        // equal, continue
                        continue;
                    }

                    if (expectedElement == null || actualElement == null)
                    {
                        differences.Add((false, elementPath, expectedElement, actualElement));
                        continue;
                    }

                    var nestedResult = DeepCompare(expectedElement, actualElement, elementPath, visited);
                    if (nestedResult.Any(x => !x.Success))
                        differences.AddRange(nestedResult);
                }

                return differences;
            }

            // Fallback: enumerator with index
            var expectedEnumerator = expectedCollection.GetEnumerator();
            var actualEnumerator = actualCollection.GetEnumerator();
            var index = 0;

            while (expectedEnumerator.MoveNext() && actualEnumerator.MoveNext())
            {
                var expectedElement = expectedEnumerator.Current;
                var actualElement = actualEnumerator.Current;

                var elementPath = JoinPath(parentPropertyName, $"[{index}]");

                // explicit null-vs-value check for enumerator fallback
                if (expectedElement == null && actualElement == null)
                {
                    index++;
                    continue;
                }

                if (expectedElement == null || actualElement == null)
                {
                    differences.Add((false, elementPath, expectedElement, actualElement));
                    index++;
                    continue;
                }

                var nestedResult = DeepCompare(expectedElement, actualElement, elementPath, visited);

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

        private static string JoinPath(string parent, string segment)
        {
            if (string.IsNullOrEmpty(parent)) return segment;
            return segment.StartsWith("[") ? parent + segment : parent + "." + segment;
        }

        private static bool IsCollectionType(Type t)
        {
            if (t == typeof(string)) return false;
            if (t.IsArray) return true;
            if (t.GetInterface(nameof(ICollection)) != null) return true;
            if (t.IsGenericType && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))) return true;
            return false;
        }

        private static Type? GetElementType(Type t)
        {
            if (t.IsArray) return t.GetElementType();

            // IList<T> / ICollection<T> / IEnumerable<T>
            var ie = t.GetInterfaces()
                      .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (ie != null)
                return ie.GetGenericArguments()[0];

            // fallback for non-generic collections
            return typeof(object);
        }
    }
}
