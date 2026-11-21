// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Builds the human-readable summary of a run: round trips per case and whether providers may be ranked.</summary>
public static class RunSummary
{
    /// <summary>Summarizes the counted cases of one provider.</summary>
    /// <param name="provider">The provider name.</param>
    /// <param name="notMeasured">The cases that are not measured on this provider, with the reason.</param>
    /// <returns>Markdown rows.</returns>
    public static string ForProvider(string provider, IReadOnlyList<(string Case, string Reason)>? notMeasured = null)
    {
        var builder = new StringBuilder();
        foreach (var key in Measurements.Keys().Order(StringComparer.Ordinal))
        {
            var stats = Measurements.For(key);
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"| {provider} | {key[(provider.Length + 1)..]} | {stats.Operations} | {stats.CommandsPerOperation:0.##} | {stats.CommandsPer1000Events:0.##} | {stats.Errors} |");
        }

        foreach (var (gapCase, reason) in notMeasured ?? [])
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"| {provider} | {gapCase} | — | — | — | {reason} |");
        }

        var scaling = ScalingReport.FromMeasurements();
        if (scaling.Length > 0) builder.AppendLine().AppendLine("## Catch-up scaling").AppendLine().Append(scaling);
        return builder.ToString();
    }

    /// <summary>Combines the providers' parts and states whether the providers are comparable.</summary>
    /// <param name="ledgers">The ledgers of the providers that ran.</param>
    /// <param name="resultDirectory">The result folder holding each provider's part.</param>
    /// <param name="cutShort">Whether the run exhausted its profile budget.</param>
    /// <returns>The summary.</returns>
    public static string Combine(IReadOnlyList<SettingsLedger> ledgers, string resultDirectory, bool cutShort = false)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Storage benchmark summary");
        if (cutShort)
        {
            var profile = StorageRunOptions.Current.Profile;
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"> **Run cut short:** the {profile.Name} profile budget of {profile.TimeoutMinutes} minutes ran out; only the cases below completed.");
        }

        builder.AppendLine()
            .AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"Durability: **{DurabilityClassifier.ComparabilityLabel(ledgers.Select(_ => _.DurabilityClass))}**")
            .AppendLine();
        foreach (var ledger in ledgers) builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"- {ledger.Provider}: {ledger.DurabilityClass}");
        builder.AppendLine().AppendLine("| provider | case | operations | commands/op | commands/1000 events | errors |").AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (var ledger in ledgers)
        {
            var part = Path.Combine(resultDirectory, ledger.Provider, "summary-part.md");
            if (File.Exists(part)) builder.Append(RowsFromPart(part));
        }

        if (cutShort) AppendUnlistedParts(builder, ledgers, resultDirectory);

        AppendArtifacts(builder, resultDirectory, "scaling.md", "Catch-up scaling");
        AppendArtifacts(builder, resultDirectory, "plans.md", "Query plans");
        return builder.ToString();
    }

    static void AppendUnlistedParts(StringBuilder builder, IReadOnlyList<SettingsLedger> ledgers, string directory)
    {
        if (!Directory.Exists(directory)) return;
        var providers = ledgers.Select(_ => _.Provider).ToHashSet(StringComparer.Ordinal);
        foreach (var part in Directory.EnumerateFiles(directory, "summary-part.md", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            if (!providers.Contains(Path.GetFileName(Path.GetDirectoryName(part))!)) builder.Append(RowsFromPart(part));
        }
    }

    static string RowsFromPart(string path) => File.ReadAllText(path).Split("\n## Catch-up scaling\n", 2, StringSplitOptions.None)[0];

    static void AppendArtifacts(StringBuilder builder, string directory, string file, string heading)
    {
        if (!Directory.Exists(directory)) return;
        var artifacts = Directory.EnumerateFiles(directory, file, SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray();
        if (artifacts.Length == 0) return;
        builder.AppendLine().AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"## {heading}").AppendLine();
        foreach (var artifact in artifacts)
        {
            builder.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"[{Path.GetFileName(Path.GetDirectoryName(artifact))}/{file}]({Path.GetRelativePath(directory, artifact).Replace('\\', '/')})").AppendLine();
            if (file == "scaling.md") builder.AppendLine(File.ReadAllText(artifact));
        }
    }
}
