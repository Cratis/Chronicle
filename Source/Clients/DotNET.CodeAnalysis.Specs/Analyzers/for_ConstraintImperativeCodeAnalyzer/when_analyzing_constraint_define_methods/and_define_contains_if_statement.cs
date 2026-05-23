// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ConstraintImperativeCodeAnalyzer.when_analyzing_constraint_define_methods;

public class and_define_contains_if_statement : given.a_constraint_imperative_code_analyzer
{
    const string Usage = """
    public class MyConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
    {
        public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder)
        {
            {|#0:if (true)
            {
                builder.PerEventSourceType();
            }|}
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ConstraintImperativeCodeAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ConstraintDefineMustNotContainImperativeCode, DiagnosticSeverity.Error, "MyConstraint"));

    [Fact] Task should_report_imperative_code_diagnostic() => _result;
}
