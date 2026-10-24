// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts.for_Options.when_parsing;

public class and_structural_inputs_are_given : Specification
{
    static readonly string[] _arguments = ["--manifest", "matrix.json", "--issues", "issues.json", "--structural", "--mode", "release"];

    Options _result;

    void Because() => _result = Options.Parse(_arguments);

    [Fact] void should_enable_structural_evaluation() => _result.Structural.ShouldBeTrue();
    [Fact] void should_keep_the_mode() => _result.Mode.ShouldEqual("release");
    [Fact] void should_not_require_job_results() => _result.Jobs.ShouldBeNull();
    [Fact] void should_not_require_trx() => _result.Trx.ShouldBeNull();
}
