using System.Runtime.CompilerServices;

namespace DeepCompare.NUnitExtension
{
    /// <summary>
    /// Compares object references for equality using reference identity.
    /// Useful when tracking visited reference pairs to detect cycles.
    /// </summary>
    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object?>
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static ReferenceEqualityComparer Instance { get; } = new();

        /// <summary>
        /// Returns true when both references point to the same object instance.
        /// </summary>
        public new bool Equals(object? x, object? y)
        {
            return ReferenceEquals(x, y);
        }

        /// <summary>
        /// Returns a hash based on the object reference (not the object.GetHashCode implementation).
        /// </summary>
        public int GetHashCode(object? obj)
        {
            return obj is null ? 0 : RuntimeHelpers.GetHashCode(obj);
        }
    }

    /// <summary>
    /// Equality comparer for pairs of object references: (expected, actual).
    /// Used by the visited HashSet that prevents infinite recursion.
    /// </summary>
    internal sealed class PairReferenceComparer : IEqualityComparer<(object? expected, object? actual)>
    {
        /// <summary>
        /// Singleton instance.
        /// </summary>
        public static PairReferenceComparer Instance { get; } = new();

        /// <summary>
        /// True when both expected and actual references are the same pair by reference.
        /// </summary>
        public bool Equals((object? expected, object? actual) x, (object? expected, object? actual) y)
        {
            return ReferenceEquals(x.expected, y.expected) && ReferenceEquals(x.actual, y.actual);
        }

        /// <summary>
        /// Combines RuntimeHelpers.GetHashCode for each referenced object into a single hash.
        /// </summary>
        public int GetHashCode((object? expected, object? actual) obj)
        {
            int h1 = obj.expected is null ? 0 : RuntimeHelpers.GetHashCode(obj.expected);
            int h2 = obj.actual is null ? 0 : RuntimeHelpers.GetHashCode(obj.actual);
            unchecked { return (h1 * 397) ^ h2; }
        }
    }
}
