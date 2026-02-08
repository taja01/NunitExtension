using System.Runtime.CompilerServices;

namespace DeepCompare.NUnitExtension
{
    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object?>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object? obj) => obj is null ? 0 : RuntimeHelpers.GetHashCode(obj);
    }

    // pair comparer (paste into your project)
    class PairReferenceComparer : IEqualityComparer<(object? expected, object? actual)>
    {
        public static PairReferenceComparer Instance { get; } = new();

        public bool Equals((object? expected, object? actual) x, (object? expected, object? actual) y)
        {
            return ReferenceEquals(x.expected, y.expected) && ReferenceEquals(x.actual, y.actual);
        }

        public int GetHashCode((object? expected, object? actual) obj)
        {
            // combine reference hashes safely
            int h1 = obj.expected is null ? 0 : RuntimeHelpers.GetHashCode(obj.expected);
            int h2 = obj.actual is null ? 0 : RuntimeHelpers.GetHashCode(obj.actual);
            unchecked { return (h1 * 397) ^ h2; }
        }
    }
}
