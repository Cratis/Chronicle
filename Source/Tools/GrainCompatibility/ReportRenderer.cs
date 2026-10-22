// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility;

/// <summary>
/// Renders the result of a comparison for a reader and for GitHub.
/// </summary>
public static class ReportRenderer
{
    /// <summary>
    /// Renders the result as text.
    /// </summary>
    /// <param name="baselineName">What the baseline is called.</param>
    /// <param name="baseline">The baseline contracts.</param>
    /// <param name="current">The current contracts.</param>
    /// <param name="breaks">The breaks found.</param>
    /// <returns>The report.</returns>
    public static string ToText(string baselineName, ContractManifest baseline, ContractManifest current, IReadOnlyList<ContractBreak> breaks)
    {
        IEnumerable<string> lines =
        [
            $"Baseline ({baselineName}): {Counts(baseline)}.",
            $"Current: {Counts(current)}.",
            string.Empty,
            .. breaks.Count == 0
                ? [$"Every grain contract of {baselineName} is still served, so silos of both can run in one cluster."]
                : Breaks(baselineName, breaks)
        ];

        return string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    /// <summary>
    /// Renders each break as a GitHub workflow error annotation.
    /// </summary>
    /// <param name="baselineName">What the baseline is called.</param>
    /// <param name="breaks">The breaks found.</param>
    /// <returns>The workflow commands, one per line.</returns>
    public static string ToWorkflowCommands(string baselineName, IReadOnlyList<ContractBreak> breaks) =>
        string.Concat(breaks.Select(_ =>
            $"::error file=Source/Kernel/Core/OrleansContracts.txt,title=Grain contract break against {Escape(baselineName)}::{Escape($"{_.Kind} in {_.Subject}: {_.Description}")}{Environment.NewLine}"));

    static string Counts(ContractManifest manifest) =>
        $"{manifest.ActiveInterfaces.Count()} grain interfaces, {manifest.ActiveInterfaces.Sum(_ => _.Methods.Count)} methods, {manifest.ActiveClasses.Count()} grain classes";

    static IEnumerable<string> Breaks(string baselineName, IReadOnlyList<ContractBreak> breaks) =>
    [
        $"{breaks.Count} change(s) mean silos of {baselineName} and of this build cannot run in one cluster:",
        .. breaks.Select(_ => $"  {_.Kind} in {_.Subject}: {_.Description}"),
        string.Empty,
        "Grain interfaces only grow within a major: keep every method, add new ones, and raise [Version] when you do.",
        "See Documentation/contributing/kernel/grain-interfaces.mdx."
    ];

    /// <summary>
    /// Encodes a value for a workflow command. Data ends at a newline and % introduces an escape, so both are encoded
    /// or a description could end the annotation early or be read as a different command.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value.</returns>
    static string Escape(string value) => value.Replace("%", "%25").Replace("\r", "%0D").Replace("\n", "%0A");
}
