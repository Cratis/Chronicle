// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts.for_Options.when_parsing;

public class and_report_inputs_are_given : Specification
{
    static readonly string[] _arguments = ["--manifest", "matrix.json", "--issues", "issues.json", "--jobs", "jobs.json", "--trx", "results", "--mode", "report", "--github"];

    Options _result;

    void Because() => _result = Options.Parse(_arguments);

    [Fact] void should_keep_the_mode() => _result.Mode.ShouldEqual("report");
    [Fact] void should_keep_the_manifest() => _result.Manifest.ShouldEqual("matrix.json");
    [Fact] void should_keep_the_issue_states() => _result.Issues.ShouldEqual("issues.json");
    [Fact] void should_keep_the_job_results() => _result.Jobs.ShouldEqual("jobs.json");
    [Fact] void should_keep_the_trx_directory() => _result.Trx.ShouldEqual("results");
    [Fact] void should_enable_workflow_commands() => _result.GitHub.ShouldBeTrue();
}
