// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Reports;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Decides whether a benchmark run counts as a success: it must have produced reports, none may have failed or hit
/// critical validation errors, and no measured operation may have failed.
/// </summary>
/// <param name="Succeeded">Whether the run succeeded.</param>
/// <param name="Reason">Why it did not, or an empty string.</param>
public sealed record RunVerdict(bool Succeeded, string Reason)
{
    /// <summary>Judges a run.</summary>
    /// <param name="summaries">The BenchmarkDotNet summaries.</param>
    /// <param name="measuredErrors">The number of failed measured operations.</param>
    /// <returns>The verdict.</returns>
    public static RunVerdict Of(IEnumerable<Summary> summaries, long measuredErrors)
    {
        var all = summaries.ToArray();
        if (all.Length == 0 || all.All(_ => _.Reports.Length == 0)) return new(false, "no benchmark produced a report");
        if (all.Any(_ => _.HasCriticalValidationErrors)) return new(false, "a benchmark had critical validation errors");
        if (all.Any(_ => _.Reports.Any(report => !report.Success))) return new(false, "a benchmark report failed");
        return measuredErrors > 0 ? new(false, $"{measuredErrors} measured operation(s) failed") : new(true, string.Empty);
    }
}
