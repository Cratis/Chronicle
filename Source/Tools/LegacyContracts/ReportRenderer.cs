// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Cratis.Chronicle.Tools.LegacyContracts;

static class ReportRenderer
{
    public static string Text(EvaluationRun run)
    {
        var output = new StringBuilder();
        output.AppendLine("## Legacy contracts");
        foreach (var row in run.Rows)
        {
            output.AppendLine($"- {row.Id}: {row.Kind} ({row.Tests} tests, {row.Planted} planted) — {row.Reason}");
        }

        output.AppendLine($"{run.Rows.Count(_ => _.Kind == RowVerdictKind.Green)} runnable green ({run.Rows.Sum(_ => _.Tests)} tests, {run.Rows.Sum(_ => _.Planted)} planted), {run.Rows.Count(_ => _.Kind == RowVerdictKind.Pending)} pending, {run.Rows.Count(_ => _.Kind == RowVerdictKind.Skipped)} skipped, {run.Rows.Count(_ => _.Kind == RowVerdictKind.Unowned)} unowned, {run.Rows.Count(_ => _.Kind == RowVerdictKind.Red)} red, {run.Rows.Count(_ => _.Kind == RowVerdictKind.StalePending)} stale pending; exit {run.ExitCode}.");
        if (run.Error is not null)
        {
            output.AppendLine($"Could not evaluate: {run.Error}");
        }

        return output.ToString();
    }

    public static string WorkflowCommands(EvaluationRun run)
    {
        var commands = run.Rows.Where(_ => _.Kind != RowVerdictKind.Green).Select(row =>
            $"::{(row.Fails(run.Mode) ? "error" : "warning")} title=Legacy contracts::{Escape($"{row.Id}: {row.Kind}: {row.Reason}")}").ToList();
        if (run.Error is not null)
        {
            commands.Add($"::error title=Legacy contracts::{Escape(run.Error)}");
        }

        return string.Join(Environment.NewLine, commands);
    }

    static string Escape(string value) => value.Replace("%", "%25", StringComparison.Ordinal).Replace("\r", "%0D", StringComparison.Ordinal).Replace("\n", "%0A", StringComparison.Ordinal);
}
