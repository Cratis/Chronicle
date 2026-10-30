// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Benchmarks.Clustering;

/// <summary>
/// Reports an activation total without truncation and the fifteen largest grain types.
/// </summary>
/// <param name="TotalActivations">The activation count across all grain types.</param>
/// <param name="ActivationsByGrainType">The top fifteen grain types and their activation counts.</param>
internal sealed record RebuildBaselineActivations(long TotalActivations, IReadOnlyDictionary<string, long> ActivationsByGrainType)
{
    /// <summary>
    /// Aggregates one silo's statistics and ranks grain types by activation count.
    /// </summary>
    /// <param name="statistics">The grain-type activation counts returned by Orleans.</param>
    /// <returns>The complete total and top fifteen types, with ordinal tie breaking.</returns>
    internal static RebuildBaselineActivations From(IEnumerable<(string GrainType, int ActivationCount)> statistics)
    {
        var counts = statistics.GroupBy(statistic => statistic.GrainType, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(statistic => (long)statistic.ActivationCount), StringComparer.Ordinal);
        var largest = counts.OrderByDescending(count => count.Value)
            .ThenBy(count => count.Key, StringComparer.Ordinal)
            .Take(15)
            .ToDictionary(count => count.Key, count => count.Value, StringComparer.Ordinal);
        return new(counts.Values.Sum(), largest);
    }
}
