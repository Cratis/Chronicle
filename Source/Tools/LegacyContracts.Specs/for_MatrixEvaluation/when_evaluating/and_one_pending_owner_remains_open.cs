// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.LegacyContracts.for_MatrixEvaluation.when_evaluating;

public class and_one_pending_owner_remains_open : Specification
{
    EvaluationRun _result;

    void Because() => _result = SelfTest.Cases.Single(_ => string.Equals(_.Name, "one_pending_owner_remains_open", StringComparison.Ordinal)).Evaluate();

    [Fact] void should_return_the_expected_exit_code() => _result.ExitCode.ShouldEqual(0);
    [Fact] void should_report_the_verdict() => _result.Rows.Single().Kind.ShouldEqual(RowVerdictKind.Pending);
}
