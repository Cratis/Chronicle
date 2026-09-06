// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility;

namespace Cratis.Chronicle.Tools.WireCompatibility.for_ReportRenderer.when_rendering;

/// <summary>
/// The run covers every released minor, so the same removed method is found once per baseline. Reporting it once
/// with the range it breaks is what makes thirty-odd baselines readable - and the range is the useful part, because
/// it says which releases actually depended on what was removed.
/// </summary>
public class and_something_broke : Specification
{
    static readonly WireIncompatibility _removedMethod =
        new(WireIncompatibilityKind.MethodRemoved, ".test.Things/Do", "The method is gone.");

    static readonly WireIncompatibility _removedField =
        new(WireIncompatibilityKind.FieldRemoved, ".test.Thing.Name", "Field number 1 is no longer declared.");

    static readonly BaselineRun _run = new(
    [
        new("16.0.0", new([_removedMethod])),
        new("16.1.0", new([_removedMethod])),
        new("16.2.0", new([_removedMethod, _removedField])),
        new("16.3.0", WireCompatibilityReport.Compatible)
    ]);

    string _text;
    string _workflowCommands;

    void Because()
    {
        _text = ReportRenderer.ToText(_run);
        _workflowCommands = ReportRenderer.ToWorkflowCommands(_run);
    }

    [Fact] void should_count_the_distinct_changes() => _text.ShouldContain("2 breaking wire changes");
    [Fact] void should_count_the_baselines_they_affect() => _text.ShouldContain("affecting 3 baselines");
    [Fact] void should_say_the_range_a_change_that_spans_baselines_breaks() => _text.ShouldContain("[breaks 16.0.0 to 16.2.0]");
    [Fact] void should_name_a_single_baseline_without_a_range() => _text.ShouldContain("[breaks 16.2.0]");
    [Fact] void should_report_a_baseline_that_is_still_served() => _text.ShouldContain("served");
    [Fact] void should_group_by_kind() => _text.ShouldContain("Methods that are gone");
    [Fact] void should_say_how_to_proceed_deliberately() => _text.ShouldContain("label the pull request 'major'");

    [Fact]
    void should_annotate_each_distinct_change_once() =>
        _workflowCommands.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length.ShouldEqual(2);
}
