// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Pure nearest-rank percentile calculation, with no fabricated empty-sample values.</summary>
internal static class Percentiles
{
    /// <summary>Returns a nearest-rank percentile.</summary>
    /// <param name="values">The observed samples.</param>
    /// <param name="percentile">The percentile in the interval (0, 1].</param>
    /// <returns>The percentile, or null for no samples.</returns>
    public static double? NearestRank(IEnumerable<double> values, double percentile)
    {
        var sorted = values.Order().ToArray();
        return sorted.Length == 0 ? null : sorted[(int)Math.Ceiling(percentile * sorted.Length) - 1];
    }
}
