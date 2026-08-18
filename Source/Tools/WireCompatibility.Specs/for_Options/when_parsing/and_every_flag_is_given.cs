// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_Options.when_parsing;

public class and_every_flag_is_given : Specification
{
    Options _result;

    void Because() => _result = Options.Parse(
    [
        "--baseline-assembly", "/baseline/Cratis.Chronicle.Contracts.dll",
        "--current", "/current/chronicle.desc",
        "--import-path", "/protos",
        "--github",
        "--allow-missing-baseline"
    ]);

    [Fact] void should_take_the_baseline_assembly() => _result.BaselineAssembly.ShouldEqual("/baseline/Cratis.Chronicle.Contracts.dll");
    [Fact] void should_take_the_import_path() => _result.ImportPath.ShouldEqual("/protos");
    [Fact] void should_emit_workflow_commands() => _result.GitHub.ShouldBeTrue();
    [Fact] void should_allow_a_missing_baseline() => _result.AllowMissingBaseline.ShouldBeTrue();
    [Fact] void should_not_take_a_major() => _result.Major.ShouldBeNull();
}
