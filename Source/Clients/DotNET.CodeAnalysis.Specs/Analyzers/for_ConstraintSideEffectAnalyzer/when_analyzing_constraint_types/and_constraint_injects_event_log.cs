// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ConstraintSideEffectAnalyzer.when_analyzing_constraint_types;

public class and_constraint_injects_event_log : given.a_constraint_side_effect_analyzer
{
    const string Usage = """
    public class MyConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
    {
        public MyConstraint({|#0:Cratis.Chronicle.EventSequences.IEventLog eventLog|}) { }
        public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder) { }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ConstraintSideEffectAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ConstraintMustNotHaveSideEffects, DiagnosticSeverity.Error, "MyConstraint", "IEventLog"));

    [Fact] Task should_report_side_effect_diagnostic() => _result;
}
