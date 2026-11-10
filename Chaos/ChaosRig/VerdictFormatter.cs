// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Formats the stable shell-facing verdict line.
/// </summary>
internal static class VerdictFormatter
{
    /// <summary>
    /// Formats a verdict and its reproduction seed.
    /// </summary>
    /// <param name="result">Evaluated violations.</param>
    /// <param name="seed">The schedule seed.</param>
    /// <param name="eventsChecked">The number of events examined.</param>
    /// <param name="observersChecked">The number of observers examined.</param>
    /// <returns>The summary line.</returns>
    public static string Format(VerdictResult result, int seed, int eventsChecked, int observersChecked) => result.IsClean
        ? $"VERDICT: clean, {eventsChecked} events checked, {observersChecked} observers checked (seed {seed})"
        : $"VERDICT: {result.Lost} lost, {result.Duplicated} duplicated, {result.Gaps} gaps, {result.ObserversBehind} observers behind, {result.ProjectionsBehind} projections behind, {result.ReducersBehind} reducers behind, {result.FailedPartitions} failed partitions, {result.ReactorDuplicates} reactor duplicates (seed {seed})";
}
