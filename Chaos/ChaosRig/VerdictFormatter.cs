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
    /// <returns>The summary line.</returns>
    public static string Format(VerdictResult result, int seed) => result.IsClean
        ? $"VERDICT: clean (seed {seed})"
        : $"VERDICT: {result.Lost} lost, {result.Duplicated} duplicated, {result.Gaps} gaps, {result.ObserversBehind} observers behind, {result.ProjectionsBehind} projections behind, {result.ReducersBehind} reducers behind, {result.FailedPartitions} failed partitions, {result.ReactorDuplicates} reactor duplicates (seed {seed})";
}
