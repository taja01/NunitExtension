using System;
using System.Collections.Generic;

namespace DeepCompare.NUnitExtension
{
    /// <summary>
    /// Options to control deep comparison behavior.
    /// </summary>
    public sealed class DeepCompareOptions
    {
        /// <summary>
        /// Collection of property paths (full path or suffix) to skip from comparison.
        /// Case-insensitive. E.g. "InnerMessage.Message" or "Message".
        /// </summary>
        public HashSet<string> SkippedProperties { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Global tolerance applied when comparing DateTime/DateTimeOffset values.
        /// If null, exact comparison is used (default).
        /// </summary>
        public TimeSpan? GlobalDateTimeTolerance { get; set; }

        /// <summary>
        /// Specific per-property tolerances for DateTime comparison.
        /// Keys may be full property path or suffix (case-insensitive).
        /// </summary>
        public Dictionary<string, TimeSpan> DateTimeTolerances { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Maximum number of differences to collect before stopping early.
        /// Defaults to 100 (to avoid excessively large diff output).
        /// </summary>
        public int MaxDifferences { get; private set; } = 100;

        /// <summary>
        /// Helper to add a property to skip.
        /// </summary>
        public DeepCompareOptions Skip(string propertyPath)
        {
            if (!string.IsNullOrEmpty(propertyPath))
            {
                SkippedProperties.Add(propertyPath);
            }

            return this;
        }

        /// <summary>
        /// Helper to set a global DateTime tolerance.
        /// </summary>
        public DeepCompareOptions WithGlobalDateTimeTolerance(TimeSpan tolerance)
        {
            GlobalDateTimeTolerance = tolerance;
            return this;
        }

        /// <summary>
        /// Helper to set a per-property DateTime tolerance.
        /// </summary>
        public DeepCompareOptions WithDateTimeTolerance(string propertyPath, TimeSpan tolerance)
        {
            if (!string.IsNullOrEmpty(propertyPath))
            {
                DateTimeTolerances[propertyPath] = tolerance;
            }

            return this;
        }

        /// <summary>
        /// Helper to configure maximum number of differences to collect.
        /// When the threshold is reached, comparison stops early.
        /// </summary>
        public DeepCompareOptions WithMaxDifferences(int max)
        {
            if (max <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(max), "Max differences must be positive.");
            }

            MaxDifferences = max;
            return this;
        }
    }
}
