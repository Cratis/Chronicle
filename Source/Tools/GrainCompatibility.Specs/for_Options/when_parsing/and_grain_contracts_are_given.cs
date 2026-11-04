// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.GrainCompatibility.for_Options.when_parsing;

public class and_grain_contracts_are_given : Specification
{
    Options _options;

    void Because() => _options = Options.Parse(["--baseline", "previous.txt", "--current", "current.txt", "--baseline-name", "19.1", "--github"]);

    [Fact] void should_preserve_the_current_grain_path() => _options.Current.ShouldEqual("current.txt");
    [Fact] void should_preserve_the_baseline_grain_path() => _options.Baseline.ShouldEqual("previous.txt");
    [Fact] void should_preserve_the_baseline_name() => _options.BaselineName.ShouldEqual("19.1");
    [Fact] void should_preserve_workflow_reporting() => _options.GitHub.ShouldBeTrue();
    [Fact] void should_leave_payload_comparison_disabled() => _options.PayloadCurrent.ShouldBeNull();
}
