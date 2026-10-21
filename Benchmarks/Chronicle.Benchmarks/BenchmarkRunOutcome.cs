// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using BenchmarkDotNet.Reports;

namespace Cratis.Chronicle.Benchmarks;

/// <summary>
/// Decides the exit code of a benchmark run from its summaries.
/// </summary>
/// <remarks>
/// BenchmarkDotNet reports a case it could not measure as NA and still exits successfully. When every case of a
/// benchmark class is NA, nothing was measured - typically because the Chronicle container never became ready - so
/// the run is reported as failed rather than as a run without results.
/// </remarks>
public static class BenchmarkRunOutcome
{
    /// <summary>
    /// Gets the exit code for a benchmark run.
    /// </summary>
    /// <param name="summaries">The summaries the run produced, one per benchmark class.</param>
    /// <param name="error">The writer that names every benchmark class that produced no results.</param>
    /// <returns>0 when every benchmark class measured at least one case, otherwise 1.</returns>
    public static int ExitCodeFor(IEnumerable<Summary> summaries, TextWriter error)
    {
        var unmeasured = summaries.Where(HasNoResults).Select(_ => _.Title).ToArray();
        foreach (var title in unmeasured)
        {
            error.WriteLine($"Benchmark run '{title}' produced no results: every case is NA or failed validation.");
        }

        return unmeasured.Length == 0 ? 0 : 1;
    }

    static bool HasNoResults(Summary summary) =>
        summary.HasCriticalValidationErrors ||
        (summary.BenchmarksCases.Length > 0 && !summary.Reports.Any(_ => _.ResultStatistics is not null));
}
