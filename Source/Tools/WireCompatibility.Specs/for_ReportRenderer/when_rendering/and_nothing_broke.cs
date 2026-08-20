// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility;

namespace Cratis.Chronicle.Tools.WireCompatibility.for_ReportRenderer.when_rendering;

public class and_nothing_broke : Specification
{
    static readonly BaselineRun _run = new(
    [
        new("16.0.0", WireCompatibilityReport.Compatible),
        new("16.1.0", WireCompatibilityReport.Compatible)
    ]);

    string _text;
    string _workflowCommands;

    void Because()
    {
        _text = ReportRenderer.ToText(_run);
        _workflowCommands = ReportRenderer.ToWorkflowCommands(_run);
    }

    [Fact] void should_say_so() => _text.ShouldContain("Every released baseline is still served.");
    [Fact] void should_list_the_first_baseline_it_checked() => _text.ShouldContain("16.0.0");
    [Fact] void should_list_the_last_baseline_it_checked() => _text.ShouldContain("16.1.0");
    [Fact] void should_mark_each_one_served() => _text.Split("served").Length.ShouldEqual(4);
    [Fact] void should_annotate_nothing() => _workflowCommands.ShouldBeEmpty();
}
