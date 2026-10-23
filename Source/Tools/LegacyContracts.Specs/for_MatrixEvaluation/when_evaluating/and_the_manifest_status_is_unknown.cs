// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts.for_MatrixEvaluation.when_evaluating;

public class and_the_manifest_status_is_unknown : Specification
{
    EvaluationRun _result;

    void Because() => _result = SelfTest.Cases.Single(_ => string.Equals(_.Name, "the_manifest_status_is_unknown", StringComparison.Ordinal)).Evaluate();

    [Fact] void should_return_the_expected_exit_code() => _result.ExitCode.ShouldEqual(2);
    [Fact] void should_explain_why_it_cannot_evaluate() => _result.Error.ShouldNotBeNull();
}
