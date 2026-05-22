// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ConstraintExpressionLambdaAnalyzer.when_analyzing_constraint_expression_lambdas;

public class and_expression_lambda_contains_method_call : given.a_constraint_expression_lambda_analyzer
{
    const string Usage = """
    public class OrderPlaced { public string OrderId { get; set; } }

    public class OrderConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
    {
        public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder) =>
            builder.Unique(u => u.On<OrderPlaced>(e => {|#0:e.OrderId.ToLower()|}));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ConstraintExpressionLambdaAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ConstraintExpressionLambdaMustOnlyAccessMembers, DiagnosticSeverity.Error, "OrderConstraint"));

    [Fact] Task should_report_imperative_expression_diagnostic() => _result;
}
