// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Formats database-produced query plans without inferring an index from SQL.</summary>
internal static partial class PlanReport
{
    [GeneratedRegex("(?:Index(?: Only)? Scan(?: Backward)? using|Bitmap Index Scan on|USING (?:COVERING )?INDEX) (?<index>\\S+)|USING (?<index>INTEGER PRIMARY KEY)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex IndexNames { get; }

    [GeneratedRegex("(?:^|\\n)\\s*(?:->\\s*)?(?:Seq Scan on|SCAN) (?<table>\\S+)[^\\r\\n]*", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, 1000)]
    private static partial Regex FullScans { get; }

    /// <summary>Formats SQL and database plans, extracting only indexes reported by the database.</summary>
    /// <param name="provider">The provider name.</param>
    /// <param name="size">The fixture size.</param>
    /// <param name="queries">The labelled SQL and plans.</param>
    /// <returns>The markdown report.</returns>
    public static string Format(string provider, int size, IEnumerable<(string Label, string Sql, string Plan)> queries)
    {
        var builder = new StringBuilder().AppendLine(CultureInfo.InvariantCulture, $"# Query plans: {provider} ({size} events)").AppendLine();
        if (provider == "postgres") builder.AppendLine("EXPLAIN replays captured values on a fresh connection; its custom plan may differ from the application's prepared generic plan.").AppendLine();
        foreach (var query in queries)
        {
            var indexes = IndexNames.Matches(query.Plan).Select(match => match.Groups["index"].Value).Distinct(StringComparer.Ordinal);
            var scans = FullScans.Matches(query.Plan).Where(match => !match.Value.Contains("USING", StringComparison.OrdinalIgnoreCase)).Select(match => $"full scan: {match.Groups["table"].Value}");
            var used = provider == "sqlserver"
                ? "see physical operators above (table.index), including scans and lookups"
                : string.Join(", ", indexes.Concat(scans));
            builder.AppendLine(CultureInfo.InvariantCulture, $"## {query.Label}").AppendLine()
                .AppendLine("```sql").AppendLine(query.Sql).AppendLine("```").AppendLine()
                .AppendLine("```text").AppendLine(query.Plan).AppendLine("```").AppendLine()
                .AppendLine(CultureInfo.InvariantCulture, $"Indexes used: {(used.Length == 0 ? "none reported" : used)}").AppendLine();
        }

        return builder.ToString();
    }
}
