using NUnit.Framework.Constraints;
using System.Collections;
using System.Reflection;

namespace DeepCompare.NUnitExtension
{
    /// <summary>
    /// Constraint that performs a deep, recursive comparison between an actual value and an expected object.
    /// Supports collections, dictionaries, nullable element handling, DateTime tolerances and property skipping.
    /// </summary>
    /// <remarks>
    /// Construct using <see cref="Matches.DeeplyWith(object, Action{DeepCompareOptions}?)"/>.
    /// The comparison runs with a per-assertion visited-pair set to protect against cycles.
    /// Differences are collected as tuples and presented via <see cref="DeeplyEqualConstraintResult"/>.
    /// </remarks>
    /// <param name="expected">The expected object to compare with.</param>
    /// <param name="options">Optional comparison options. When null, defaults are used.</param>
    public partial class DeeplyEqualConstraint(object expected, DeepCompareOptions? options = null) : Constraint
    {
        private readonly object _expected = expected;
        private readonly DeepCompareOptions _options = options ?? new DeepCompareOptions();

        private static readonly System.Collections.Concurrent.ConcurrentDictionary<Type, PropertyInfo[]> _propsCache = new();

        private static PropertyInfo[] GetPropertiesCached(Type t) =>
            _propsCache.GetOrAdd(t, _ => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        /// <summary>
        /// Short textual description of the constraint used by NUnit.
        /// </summary>
        public override string Description => "Deeply equal objects";

        /// <summary>
        /// Get max differences from options for use in result reporting.
        /// </summary>
        public int MaxDifferences => _options.MaxDifferences;

        // --- helpers for early-exit when MaxDifferences reached ---
        /// <summary>
        /// Adds a single difference and returns true when the configured max-differences threshold has been reached.
        /// Internal helper used to implement early exit.
        /// </summary>
        private bool TryAddDifference(List<(bool Success, string PropertyName, object? ExpectedValue, object? ActualValue)> diffs,
            (bool Success, string PropertyName, object? ExpectedValue, object? ActualValue) diff)
        {
            diffs.Add(diff);
            return diffs.Count >= _options.MaxDifferences;
        }

        /// <summary>
        /// Adds a sequence of differences and returns true when the configured max-differences threshold has been reached.
        /// Internal helper used to implement early exit.
        /// </summary>
        private bool TryAddRange(List<(bool Success, string PropertyName, object? ExpectedValue, object? ActualValue)> diffs,
            IEnumerable<(bool Success, string PropertyName, object? ExpectedValue, object? ActualValue)> items)
        {
            foreach (var item in items)
            {
                diffs.Add(item);
                if (diffs.Count >= _options.MaxDifferences)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Core recursive comparison routine.
        /// </summary>
        /// <param name="expected">Expected object (may be null).</param>
        /// <param name="actual">Actual object to compare (may be null).</param>
        /// <param name="parentPropertyName">Current property path used for diagnostics (dot/bracket notation).</param>
        /// <param name="visited">Set of (expected,actual) reference pairs to detect and avoid cycles.</param>
        /// <returns>List of comparison result tuples; empty list means objects considered equal.</returns>
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
                if (TryAddDifference(differences, (false, parentPropertyName, expected, actual)))
                    return differences;
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
                        if (TryAddDifference(differences, (false, $"Different Type: {parentPropertyName}".TrimStart('.'), $"{expectedType.Name}", $"{actualType.Name}"))
                            ) return differences;
                        return differences;
                    }
                }
                // If both are dictionary-like, allow dictionary comparison despite different concrete types
                else if (IsDictionaryType(expectedType) && IsDictionaryType(actualType))
                {
                    // allow falling through to dictionary comparison below
                }
                else
                {
                    if (TryAddDifference(differences, (false, $"Different Type: {parentPropertyName}".TrimStart('.'), $"{expectedType.Name}", $"{actualType.Name}"))
                        ) return differences;
                    return differences;
                }
            }

            // For reference types (not value types and not string) detect cycles using the visited pair set.
            var isReferenceType = !expectedType.IsValueType && expected is not string;
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
                        if (TryAddDifference(differences, (false, parentPropertyName, expected, actual)))
                            return differences;
                    }
                    return differences;
                }

                if (!Equals(expected, actual))
                {
                    if (TryAddDifference(differences, (false, parentPropertyName, expected, actual)))
                        return differences;
                }
                return differences;
            }

            // Dictionary handling (generic IDictionary<,> or non-generic IDictionary)
            if (IsDictionaryType(expectedType) && IsDictionaryType(actualType))
            {
                var nested = CompareDictionaries(expected, actual, parentPropertyName, visited);
                if (nested.Any(x => !x.Success))
                {
                    if (TryAddRange(differences, nested))
                        return differences;
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
                    {
                        if (TryAddRange(differences, nestedResult))
                            return differences;
                    }
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
                object? actualValue = actualProp?.GetValue(actual);

                // both null -> continue
                if (expectedValue == null && actualValue == null)
                    continue;

                // one null -> difference
                if (expectedValue == null || actualValue == null)
                {
                    if (TryAddDifference(differences, (false, fullName, expectedValue, actualValue)))
                        return differences;
                    continue;
                }

                // If collection
                if (expectedValue is ICollection expectedColl && actualValue is ICollection actualColl)
                {
                    var nested = CompareLists(expectedColl, actualColl, fullName, visited);
                    if (nested.Any(x => !x.Success))
                    {
                        if (TryAddRange(differences, nested))
                            return differences;
                    }
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
                            if (TryAddDifference(differences, (false, fullName, expectedValue, actualValue)))
                                return differences;
                        }
                        continue;
                    }

                    if (!Equals(expectedValue, actualValue))
                    {
                        if (TryAddDifference(differences, (false, fullName, expectedValue, actualValue)))
                            return differences;
                    }
                    continue;
                }

                // Complex object -> recurse, pass visited set to detect cycles
                var nestedResult = DeepCompare(expectedValue, actualValue, fullName, visited);
                if (nestedResult.Any(x => !x.Success))
                {
                    if (TryAddRange(differences, nestedResult))
                        return differences;
                }
            }

            return differences;
        }

        /// <summary>
        /// Compare two dictionary-like objects by keys and values. Produces key-aware paths like "[key]" for diagnostics.
        /// </summary>
        private List<(bool Success, string PropertyName, object? ExpectedValue, object? ActualValue)> CompareDictionaries(
            object expectedDictObj,
            object actualDictObj,
            string parentPropertyName,
            HashSet<(object? expected, object? actual)> visited)
        {
            var differences = new List<(bool, string, object?, object?)>();

            // Track pair to prevent infinite recursion but do not short-circuit
            var dictPair = (expectedDictObj as object, actualDictObj as object);
            if (!visited.Contains(dictPair))
                visited.Add(dictPair);

            // Non-generic IDictionary fast path
            if (expectedDictObj is IDictionary expectedNonGen && actualDictObj is IDictionary actualNonGen)
            {
                if (expectedNonGen.Count != actualNonGen.Count)
                {
                    if (TryAddDifference(differences, (false, JoinPath(parentPropertyName, "Count"), $"Count {expectedNonGen.Count}", $"Count {actualNonGen.Count}")))
                        return differences;
                    // continue to find key diffs
                }

                // Build fast lookup of actual keys -> values (object equality)
                var actualLookup = new Dictionary<object, object>(actualNonGen.Count, new ObjectKeyComparer());
                foreach (var key in actualNonGen.Keys)
                    actualLookup[key] = actualNonGen[key];

                // Compare expected keys
                foreach (var key in expectedNonGen.Keys)
                {
                    var keyPath = JoinPath(parentPropertyName, $"[{FormatKey(key)}]");
                    if (!actualLookup.TryGetValue(key, out var actualVal))
                    {
                        if (TryAddDifference(differences, (false, keyPath, expectedNonGen[key], null)))
                            return differences;
                        continue;
                    }

                    var nested = DeepCompare(expectedNonGen[key], actualVal, keyPath, visited);
                    if (nested.Any(x => !x.Success))
                    {
                        if (TryAddRange(differences, nested))
                            return differences;
                    }
                }

                // Extra keys in actual
                foreach (var key in actualNonGen.Keys)
                {
                    if (!expectedNonGen.Contains(key))
                    {
                        var keyPath = JoinPath(parentPropertyName, $"[{FormatKey(key)}]");
                        if (TryAddDifference(differences, (false, keyPath, null, actualNonGen[key])))
                            return differences;
                    }
                }

                return differences;
            }

            // Generic dictionaries or enumerable-of-KVP fallback
            var expectedEntries = EnumerateKeyValuePairs(expectedDictObj).ToList();
            var actualEntries = EnumerateKeyValuePairs(actualDictObj).ToList();

            if (expectedEntries.Count != actualEntries.Count)
            {
                if (TryAddDifference(differences, (false, JoinPath(parentPropertyName, "Count"), $"Count {expectedEntries.Count}", $"Count {actualEntries.Count}")))
                    return differences;
                // continue to find key diffs
            }

            // Build actual lookup using object equality for keys (O(n))
            var actualLookupGeneric = new Dictionary<object, object>(new ObjectKeyComparer());
            foreach (var (k, v) in actualEntries)
                actualLookupGeneric[k] = v;

            // Compare expected entries using lookup
            foreach (var (eKey, eValue) in expectedEntries)
            {
                var keyPath = JoinPath(parentPropertyName, $"[{FormatKey(eKey)}]");
                if (!actualLookupGeneric.TryGetValue(eKey, out var aValue))
                {
                    if (TryAddDifference(differences, (false, keyPath, eValue, null)))
                        return differences;
                    continue;
                }

                var nested = DeepCompare(eValue, aValue, keyPath, visited);
                if (nested.Any(x => !x.Success))
                {
                    if (TryAddRange(differences, nested))
                        return differences;
                }
            }

            // Find extra keys in actual
            foreach (var (aKey, aValue) in actualEntries)
            {
                if (!expectedEntries.Any(e => KeysEqual(e.key, aKey)))
                {
                    var keyPath = JoinPath(parentPropertyName, $"[{FormatKey(aKey)}]");
                    if (TryAddDifference(differences, (false, keyPath, null, aValue)))
                        return differences;
                }
            }

            return differences;
        }

        // Helper to enumerate KeyValuePair entries for generic IDictionary<TKey, TValue> or any enumerable of KeyValuePair, etc.
        private static IEnumerable<(object? key, object? value)> EnumerateKeyValuePairs(object dictLike)
        {
            if (dictLike is null) yield break;

            if (dictLike is IDictionary nonGen)
            {
                foreach (var key in nonGen.Keys)
                    yield return (key, nonGen[key]);
                yield break;
            }

            if (dictLike is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (item == null) continue;
                    var t = item.GetType();
                    var keyProp = t.GetProperty("Key");
                    var valueProp = t.GetProperty("Value");
                    if (keyProp != null && valueProp != null)
                    {
                        yield return (keyProp.GetValue(item), valueProp.GetValue(item));
                    }
                }
            }
        }

        private static bool KeysEqual(object? a, object? b)
        {
            if (a == null && b == null) return true;
            if (a == null || b == null) return false;
            return a.Equals(b);
        }

        private static string FormatKey(object? key)
        {
            if (key == null) return "null";
            return key is string s ? s : key.ToString() ?? "key";
        }

        private List<(bool Success, string PropertyName, object? ExpectedValue, object? ActualValue)> CompareLists(
            ICollection expectedCollection,
            ICollection actualCollection,
            string parentPropertyName,
            HashSet<(object? expected, object? actual)> visited)
        {
            var differences = new List<(bool, string, object?, object?)>();

            // Normalize parent path (do not trim leading/trailing bracket segments)
            parentPropertyName ??= string.Empty;

            // Track collection pair to avoid infinite recursion but do NOT short-circuit comparison
            if (expectedCollection is object && actualCollection is object)
            {
                var collectionPair = (expectedCollection as object, actualCollection as object);
                visited.Add(collectionPair);
            }

            if (expectedCollection?.Count != actualCollection?.Count)
            {
                if (TryAddDifference(differences, (false, JoinPath(parentPropertyName, "Count"), $"Count {expectedCollection?.Count}", $"Count {actualCollection?.Count}")))
                    return differences;
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
                        if (TryAddDifference(differences, (false, elementPath, expectedElement, actualElement)))
                            return differences;
                        continue;
                    }

                    var nestedResult = DeepCompare(expectedElement, actualElement, elementPath, visited);
                    if (nestedResult.Any(x => !x.Success))
                    {
                        if (TryAddRange(differences, nestedResult))
                            return differences;
                    }
                }

                return differences;
            }

            // Fallback: enumerator with index
            var expectedEnumerator = expectedCollection?.GetEnumerator();
            var actualEnumerator = actualCollection?.GetEnumerator();
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
                    if (TryAddDifference(differences, (false, elementPath, expectedElement, actualElement)))
                        return differences;
                    index++;
                    continue;
                }

                var nestedResult = DeepCompare(expectedElement, actualElement, elementPath, visited);

                if (nestedResult.Any(x => !x.Success))
                {
                    if (TryAddRange(differences, nestedResult))
                        return differences;
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

        private static bool IsDateTimeLike(Type t)
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
            return segment.StartsWith("[", StringComparison.OrdinalIgnoreCase) ? parent + segment : parent + "." + segment;
        }

        private static bool IsCollectionType(Type t)
        {
            if (t == typeof(string)) return false;
            if (t.IsArray) return true;
            if (t.GetInterface(nameof(ICollection)) != null) return true;
            if (t.IsGenericType && t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>))) return true;
            return false;
        }

        private static bool IsDictionaryType(Type t)
        {
            if (t == typeof(string)) return false;
            if (t.GetInterface(nameof(IDictionary)) != null) return true;
            if (t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>))) return true;
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

        private sealed class ObjectKeyComparer : IEqualityComparer<object?>
        {
            public new bool Equals(object? x, object? y) => object.Equals(x, y);

            public int GetHashCode(object? obj) => obj?.GetHashCode() ?? 0;
        }
    }
}
