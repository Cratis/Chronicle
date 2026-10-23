// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Counts realized consecutive same-source runs, rather than requested run sizes.
/// </summary>
internal static class ReplayShapeRunHistogram
{
    /// <summary>
    /// Counts exact run lengths across an ordered event-source sequence.
    /// </summary>
    /// <param name="sources">The actual workload order.</param>
    /// <returns>Exact lengths mapped to the number of runs with that length.</returns>
    internal static IReadOnlyDictionary<int, int> Count(IEnumerable<string> sources)
    {
        var histogram = new SortedDictionary<int, int>();
        string? previous = null;
        var length = 0;
        foreach (var source in sources)
        {
            if (length > 0 && source != previous)
            {
                histogram[length] = histogram.GetValueOrDefault(length) + 1;
                length = 0;
            }
            previous = source;
            length++;
        }
        if (length > 0) histogram[length] = histogram.GetValueOrDefault(length) + 1;
        return histogram;
    }
}
