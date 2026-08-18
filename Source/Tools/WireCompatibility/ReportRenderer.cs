// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using Cratis.Chronicle.Compatibility;

namespace Cratis.Chronicle.Tools.WireCompatibility;

/// <summary>
/// Renders what a run found, for whoever has to act on it.
/// </summary>
public static class ReportRenderer
{
    /// <summary>
    /// Renders the run as plain text for a terminal or a build log.
    /// </summary>
    /// <param name="run">The run to render.</param>
    /// <returns>The rendered report.</returns>
    public static string ToText(BaselineRun run)
    {
        var builder = new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"Checked against {Count(run.Results.Count, "released baseline")}:")
            .AppendLine();

        foreach (var result in run.Results)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"  {result.Version,-12} {Verdict(result)}");
        }

        builder.AppendLine();

        if (run.IsCompatible)
        {
            return builder.Append("Every released baseline is still served.").ToString();
        }

        var findings = run.Findings.ToList();
        builder
            .AppendLine(CultureInfo.InvariantCulture, $"{Count(findings.Count, "breaking wire change")}, affecting {Count(run.Broken.Count(), "baseline")}:")
            .AppendLine();

        foreach (var group in findings.GroupBy(_ => _.Incompatibility.Kind).OrderBy(_ => _.Key))
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"  {Humanize(group.Key)}");

            foreach (var finding in group)
            {
                builder
                    .AppendLine(CultureInfo.InvariantCulture, $"    {finding.Incompatibility.Path}   [breaks {finding.Range}]")
                    .AppendLine(CultureInfo.InvariantCulture, $"      {finding.Incompatibility.Description}");
            }

            builder.AppendLine();
        }

        return builder
            .Append("Everything released within a major has to keep serving every release before it. Restore what was removed, or label the pull request 'major'.")
            .ToString();
    }

    /// <summary>
    /// Renders the run as GitHub workflow commands, so each change shows up as an annotation.
    /// </summary>
    /// <param name="run">The run to render.</param>
    /// <returns>The rendered workflow commands, empty when nothing broke.</returns>
    public static string ToWorkflowCommands(BaselineRun run)
    {
        var builder = new StringBuilder();

        foreach (var finding in run.Findings)
        {
            builder.AppendLine(CultureInfo.InvariantCulture, $"::error title=Breaking wire change vs {Escape(finding.Range)}::{Escape(finding.Incompatibility.Path)} - {Escape(finding.Incompatibility.Description)}");
        }

        return builder.ToString();
    }

    static string Verdict(BaselineResult result) =>
        result.Report.IsCompatible
            ? "served"
            : $"{Count(result.Report.Incompatibilities.Count, "breaking change")}";

    static string Count(int count, string noun) =>
        $"{count.ToString(CultureInfo.InvariantCulture)} {noun}{(count == 1 ? string.Empty : "s")}";

    static string Humanize(WireIncompatibilityKind kind) => kind switch
    {
        WireIncompatibilityKind.ServiceRemoved => "Services that are gone",
        WireIncompatibilityKind.MethodRemoved => "Methods that are gone",
        WireIncompatibilityKind.MethodSignatureChanged => "Methods that take or return something else",
        WireIncompatibilityKind.MethodStreamingChanged => "Methods whose call shape changed",
        WireIncompatibilityKind.MessageRemoved => "Messages that are gone",
        WireIncompatibilityKind.FieldRemoved => "Fields that are gone",
        WireIncompatibilityKind.FieldTypeChanged => "Fields that changed type",
        WireIncompatibilityKind.FieldLabelChanged => "Fields that changed between singular and repeated",
        WireIncompatibilityKind.FieldRenamed => "Fields that were renamed",
        WireIncompatibilityKind.FieldOneOfChanged => "Fields that moved into or out of a oneof",
        WireIncompatibilityKind.EnumRemoved => "Enums that are gone",
        WireIncompatibilityKind.EnumValueRemoved => "Enum values that are gone",
        WireIncompatibilityKind.EnumValueRenamed => "Enum values that were renamed",
        _ => kind.ToString()
    };

    /// <summary>
    /// Makes a value safe to carry inside a workflow command.
    /// </summary>
    /// <param name="value">The value to escape.</param>
    /// <returns>The escaped value.</returns>
    /// <remarks>
    /// A workflow command ends at the first newline, and a '::' inside one would start another.
    /// </remarks>
    static string Escape(string value) =>
        value.Replace("\r", string.Empty, StringComparison.Ordinal).Replace('\n', ' ').Replace("::", ":", StringComparison.Ordinal);
}
