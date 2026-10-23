// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts.for_MatrixEvaluation.when_evaluating;

public class and_a_pending_row_has_no_required_proofs : Specification
{
    EvaluationRun _result;

    void Because() => _result = SelfTest.Cases.Single(_ => string.Equals(_.Name, "a_pending_row_has_no_required_proofs", StringComparison.Ordinal)).Evaluate();

    [Fact] void should_not_evaluate() => _result.ExitCode.ShouldEqual(2);
    [Fact] void should_explain_the_input_failure() => _result.Error.ShouldNotBeNull();
}
