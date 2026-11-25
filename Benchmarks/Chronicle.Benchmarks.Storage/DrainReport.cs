// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>
/// Formats page-position attribution without mixing terminal probes into page percentiles.
/// </summary>
internal static class DrainReport
{
    /// <summary>
    /// Compares chronological first/last quarters using nearest-rank percentiles.
    /// </summary>
    /// <param name="size">The seeded event count.</param>
    /// <param name="pages">Cursor calls in drain order.</param>
    /// <param name="total">Wall time including cursor creation and page enumeration, excluding seeding.</param>
    /// <returns>The summary and per-call markdown table.</returns>
    public static string Format(int size, IReadOnlyList<DrainPage> pages, TimeSpan total)
    {
        var data = pages.Where(page => page.Events > 0).ToArray();
        var times = data.Select(page => page.Milliseconds).ToArray();
        var quarter = Math.Max(1, data.Length / 4);
        var first = OperationStats.Percentile(times.Take(quarter).ToArray(), 50);
        var last = OperationStats.Percentile(times.TakeLast(quarter).ToArray(), 50);
        var pageSizes = string.Join(", ", data.Select(page => page.Events).Distinct().Order());
        var builder = new StringBuilder().AppendLine(CultureInfo.InvariantCulture, $"## {size} events").AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Pages: {data.Length}; page sizes (events): {pageSizes}; events read: {data.Sum(page => page.Events)}.")
            .AppendLine(CultureInfo.InvariantCulture, $"MoveNext page ms: median={OperationStats.Percentile(times, 50):F3}; p90={OperationStats.Percentile(times, 90):F3}; max={times.DefaultIfEmpty().Max():F3}.")
            .AppendLine(CultureInfo.InvariantCulture, $"Quarter medians ({quarter} pages each): first={first:F3} ms; last={last:F3} ms.")
            .AppendLine(CultureInfo.InvariantCulture, $"Total: {total.TotalMilliseconds:F3} ms wall; {pages.Sum(page => page.Milliseconds):F3} ms MoveNext (including terminal probe).")
            .AppendLine().AppendLine("| Page (zero-based) | Starting sequence | Events | MoveNext ms |")
            .AppendLine("| --- | --- | --- | --- | ");
        foreach (var page in pages)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"| {page.Index} | {page.Start} | {page.Events} | {page.Milliseconds:F3} |");
        }

        return builder.AppendLine().ToString();
    }
}
