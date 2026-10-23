// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Samples run lengths from weighted, non-overlapping maximum-length buckets.
/// </summary>
/// <param name="Buckets">Ascending maximum lengths and their shares of runs.</param>
internal sealed record ReplayShapeRunLengthMix(IReadOnlyDictionary<int, double> Buckets)
{
    /// <summary>
    /// Parses strictly ascending buckets with positive, finite weights.
    /// </summary>
    /// <param name="value">Comma-separated maximum:weight pairs, or null for fixed lengths.</param>
    /// <returns>The parsed mix, or null when none was configured.</returns>
    /// <exception cref="ArgumentException">The configured mix is invalid.</exception>
    internal static ReplayShapeRunLengthMix? Parse(string? value)
    {
        if (value is null) return null;
        var buckets = new Dictionary<int, double>();
        var previous = 0;
        foreach (var entry in value.Split(','))
        {
            var pair = entry.Split(':', StringSplitOptions.TrimEntries);
            if (pair.Length != 2 ||
                !int.TryParse(pair[0], NumberStyles.None, CultureInfo.InvariantCulture, out var maximum) || maximum <= previous ||
                !double.TryParse(pair[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var weight) || !double.IsFinite(weight) || weight <= 0)
            {
                throw new ArgumentException("REPLAY_RUN_LENGTH_MIX must contain ascending positive maximum:weight pairs with finite positive weights.");
            }
            buckets.Add(maximum, weight);
            previous = maximum;
        }
        if (!double.IsFinite(buckets.Values.Sum())) throw new ArgumentException("REPLAY_RUN_LENGTH_MIX total weight must be finite.");
        return new(buckets);
    }

    /// <summary>
    /// Chooses a bucket by run weight, then a length uniformly within that bucket.
    /// </summary>
    /// <param name="random">The scenario's seeded generator.</param>
    /// <returns>The sampled positive run length.</returns>
    /// <exception cref="InvalidOperationException">The mix has no buckets.</exception>
    internal int Sample(Random random)
    {
        var draw = random.NextDouble() * Buckets.Values.Sum();
        long minimum = 1;
        foreach (var bucket in Buckets)
        {
            if (draw < bucket.Value || bucket.Key == Buckets.Keys.Last())
            {
                return (int)random.NextInt64(minimum, (long)bucket.Key + 1);
            }
            draw -= bucket.Value;
            minimum = (long)bucket.Key + 1;
        }
        throw new InvalidOperationException("The run-length mix has no buckets.");
    }
}
