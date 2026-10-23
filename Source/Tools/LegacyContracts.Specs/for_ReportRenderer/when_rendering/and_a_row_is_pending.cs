// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts.for_ReportRenderer.when_rendering;

public class and_a_row_is_pending : Specification
{
    EvaluationRun _run;

    void Because() => _run = new(0, [new("C1", RowVerdictKind.Pending, 0, 0, "missing%proof\nnext")], null);

    [Fact] void should_warn_in_report_mode() => ReportRenderer.WorkflowCommands(_run).StartsWith("::warning", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_fail_in_release_mode() => ReportRenderer.WorkflowCommands(_run with { Mode = "release" }).StartsWith("::error", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_escape_workflow_command_data() => ReportRenderer.WorkflowCommands(_run).Contains("missing%25proof%0Anext", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_show_pending_counts() => ReportRenderer.Text(_run).Contains("1 pending", StringComparison.Ordinal).ShouldBeTrue();
}
