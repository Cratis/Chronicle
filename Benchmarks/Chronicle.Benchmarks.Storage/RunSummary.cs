// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Builds the human-readable summary of a run: round trips per case and whether providers may be ranked.</summary>
public static class RunSummary
{
    /// <summary>Summarizes the counted cases of one provider.</summary>
    /// <param name="provider">The provider name.</param>
    /// <returns>Markdown rows.</returns>
    public static string ForProvider(string provider)
    {
        var builder = new StringBuilder();
        foreach (var key in Measurements.Keys().Order(StringComparer.Ordinal))
        {
            var stats = Measurements.For(key);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"| {provider} | {key[(provider.Length + 1)..]} | {stats.Operations} | {stats.CommandsPerOperation:0.##} | {stats.CommandsPer1000Events:0.##} | {stats.Errors} |");
        }

        return builder.ToString();
    }

    /// <summary>Combines the providers' parts and states whether the providers are comparable.</summary>
    /// <param name="ledgers">The ledgers of the providers that ran.</param>
    /// <param name="resultDirectory">The result folder holding each provider's part.</param>
    /// <returns>The summary.</returns>
    public static string Combine(IReadOnlyList<SettingsLedger> ledgers, string resultDirectory)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Storage benchmark summary")
            .AppendLine()
            .AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Durability: **{DurabilityClassifier.ComparabilityLabel(ledgers.Select(_ => _.DurabilityClass))}**")
            .AppendLine();
        foreach (var ledger in ledgers) builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"- {ledger.Provider}: {ledger.DurabilityClass}");
        builder.AppendLine().AppendLine("| provider | case | operations | commands/op | commands/1000 events | errors |").AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var ledger in ledgers)
        {
            var part = Path.Combine(resultDirectory, ledger.Provider, "summary-part.md");
            if (File.Exists(part)) builder.Append(File.ReadAllText(part));
        }

        return builder.ToString();
    }
}
