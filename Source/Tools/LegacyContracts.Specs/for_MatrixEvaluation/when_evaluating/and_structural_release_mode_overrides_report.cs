// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Tools.LegacyContracts.for_MatrixEvaluation.when_evaluating;

public class and_structural_release_mode_overrides_report : Specification
{
    EvaluationRun _result;

    void Because()
    {
        var clean = EvaluationFixture.Clean();
        var pending = clean.WithRow(clean.Matrix.Rows[0] with { Status = "pending-owner", RequiredProofs = ["Prove refusal before adoption."] });
        _result = EvaluationRun.ReadStructural(JsonSerializer.Serialize(pending.Matrix), JsonSerializer.Serialize(pending.Issues), "release");
    }

    [Fact] void should_fail_the_gate() => _result.ExitCode.ShouldEqual(1);
    [Fact] void should_keep_the_pending_verdict() => _result.Rows.Single().Kind.ShouldEqual(RowVerdictKind.Pending);
    [Fact] void should_use_release_enforcement() => _result.Mode.ShouldEqual("release");
}
