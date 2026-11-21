// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Formats catch-up scaling separately from the stable six-column operation table.</summary>
public static class ScalingReport
{
    /// <summary>Formats completed counted-pass results.</summary>
    /// <returns>The scaling table, or empty when no family has two completed sizes.</returns>
    public static string FromMeasurements() => Format(Measurements.Keys().Select(key =>
    {
        var parts = key.Split('/');
        var report = JsonSerializer.SerializeToElement(Measurements.For(key).ToReport());
        return (Case: string.Join('/', parts.Take(2)), Size: int.TryParse(parts[^1], CultureInfo.InvariantCulture, out var size) ? size : 0,
            P50: report.GetProperty("latencyMs").GetProperty("p50").GetDouble());
    }).Where(_ => _.Case.Contains("/catch-up-", StringComparison.Ordinal) && _.Size > 0));

    /// <summary>Formats per-size p50 latency and normalized growth relative to the smallest size.</summary>
    /// <param name="samples">The completed measurements, keyed by provider and catch-up family.</param>
    /// <returns>The markdown.</returns>
    public static string Format(IEnumerable<(string Case, int Size, double P50)> samples)
    {
        var builder = new StringBuilder();
        foreach (var family in samples.GroupBy(_ => _.Case).OrderBy(_ => _.Key, StringComparer.Ordinal))
        {
            var ordered = family.Where(_ => _.Size > 0).OrderBy(_ => _.Size).ToArray();
            if (ordered.Select(_ => _.Size).Distinct().Count() < 2) continue;
            builder.AppendLine(CultureInfo.InvariantCulture, $"### {family.Key}").AppendLine()
                .AppendLine("| Size | p50 ms | ms / 1,000 events | Ratio to smallest | Growth |")
                .AppendLine("| ---: | ---: | ---: | ---: | --- |");
            var baseline = ordered[0].P50 * 1000 / ordered[0].Size;
            foreach (var sample in ordered)
            {
                var normalized = sample.P50 * 1000 / sample.Size;
                var ratio = baseline > 0 ? normalized / baseline : double.NaN;
                builder.AppendLine(CultureInfo.InvariantCulture, $"| {sample.Size} | {sample.P50:0.###} | {normalized:0.###} | {ratio:0.###} | {(ratio > 1.3 ? "super-linear" : string.Empty)} |");
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }
}
