// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility.for_Options.when_parsing;

public class and_a_baseline_major_is_given : Specification
{
    Options _result;

    void Because() => _result = Options.Parse(["--major", "16", "--current", "/somewhere/chronicle.desc"]);

    [Fact] void should_take_the_major() => _result.Major.ShouldEqual(16);
    [Fact] void should_take_the_current_contract() => _result.Current.ShouldEqual("/somewhere/chronicle.desc");
    [Fact] void should_default_the_import_path_to_where_the_current_contract_lives() => _result.ImportPath.ShouldEqual("/somewhere");
    [Fact] void should_not_emit_workflow_commands() => _result.GitHub.ShouldBeFalse();
    [Fact] void should_not_allow_a_missing_baseline() => _result.AllowMissingBaseline.ShouldBeFalse();
}
