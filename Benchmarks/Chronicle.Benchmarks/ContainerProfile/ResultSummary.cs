// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Cratis.Chronicle.Benchmarks.ContainerProfile;

/// <summary>Produces machine-readable results and an untraced-only CSV summary.</summary>
internal static class ResultSummary
{
    /// <summary>Writes all rows and median/range summaries, counting unsuccessful cells explicitly.</summary>
    /// <param name="directory">The session results directory.</param>
    /// <param name="results">All repetitions.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The write task.</returns>
    public static async Task Write(string directory, IReadOnlyList<ProfileResult> results, CancellationToken cancellationToken)
    {
        await ResultFiles.Write(Path.Combine(directory, "results.json"), results, cancellationToken);
        var text = new StringBuilder("cell,repetitions,completed,unsuccessful,oom,cold_ready_ms_median,cold_ready_ms_min,cold_ready_ms_max,append_eps_median,append_eps_min,append_eps_max,observer_catchup_ms_median,observer_catchup_ms_min,observer_catchup_ms_max,idle_rss_kb_median,peak_rss_kb_median,vmhwm_kb_max,stop_ms_median\n");
        foreach (var group in results.Where(result => !result.TraceRepetition).GroupBy(result => result.Cell))
        {
            var successful = group.Where(result => result.Status == "completed").ToArray();
            var values = new List<string> { group.Key, Number(group.Count()), Number(successful.Length), Number(group.Count(result => result.Status != "completed")), Number(group.Count(result => result.Status == "oom")) };
            values.AddRange(Spread(successful.Select(result => result.ColdReadyMs)));
            values.AddRange(Spread(successful.Select(result => result.Workload?.AppendEventsPerSecond)));
            values.AddRange(Spread(successful.Select(result => result.Workload?.ObserverCatchUpMs)));
            values.Add(Format(Median(successful.Select(result => (double?)result.Idle?.RssKb))));
            values.Add(Format(Median(successful.Select(result => result.Memory.Select(sample => (double?)sample.RssKb).DefaultIfEmpty().Max()))));
            values.Add(Format(successful.SelectMany(result => result.Memory).Select(sample => (double?)sample.HighWaterKb).DefaultIfEmpty().Max()));
            values.Add(Format(Median(successful.Select(result => result.StopMs))));
            text.AppendJoin(',', values).AppendLine();
        }
        await File.WriteAllTextAsync(Path.Combine(directory, "summary.csv"), text.ToString(), cancellationToken);
    }

    static IEnumerable<string> Spread(IEnumerable<double?> values)
    {
        var sorted = values.OfType<double>().Order().ToArray();
        return [Format(Median(values)), Format(sorted.Length > 0 ? sorted[0] : null), Format(sorted.Length > 0 ? sorted[^1] : null)];
    }

    static double? Median(IEnumerable<double?> values)
    {
        var sorted = values.OfType<double>().Order().ToArray();
        return sorted.Length == 0 ? null : (sorted[(sorted.Length - 1) / 2] + sorted[sorted.Length / 2]) / 2;
    }

    static string Format(double? value) => value?.ToString("F3", CultureInfo.InvariantCulture) ?? string.Empty;

    static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}
