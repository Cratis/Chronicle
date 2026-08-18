// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compatibility;

namespace Cratis.Chronicle.Tools.WireCompatibility.for_ReportRenderer.when_rendering;

/// <summary>
/// A workflow command ends at the first newline and a '::' inside one starts another, so an unescaped description
/// would silently truncate the annotation or inject a second command into the log.
/// </summary>
public class and_a_description_would_break_the_workflow_command : Specification
{
    string _result;

    void Because() => _result = ReportRenderer.ToWorkflowCommands(new(
    [
        new("16.0.0", new([new(WireIncompatibilityKind.MethodRemoved, ".test.Things/Do", "Line one\nline two, and a ::marker.")]))
    ]));

    [Fact] void should_stay_one_command() => _result.TrimEnd('\n').ShouldNotContain("\n");
    [Fact] void should_keep_both_lines_of_text() => _result.ShouldContain("Line one line two");
    [Fact] void should_not_start_another_command() => _result.ShouldNotContain("::marker");
}
