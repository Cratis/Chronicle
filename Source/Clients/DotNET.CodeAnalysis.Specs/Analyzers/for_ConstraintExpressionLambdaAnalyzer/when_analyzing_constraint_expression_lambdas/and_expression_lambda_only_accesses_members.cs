// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ConstraintExpressionLambdaAnalyzer.when_analyzing_constraint_expression_lambdas;

public class and_expression_lambda_only_accesses_members : given.a_constraint_expression_lambda_analyzer
{
    const string Usage = """
    public class OrderPlaced { public string OrderId { get; set; } }

    public class OrderConstraint : Cratis.Chronicle.Events.Constraints.IConstraint
    {
        public void Define(Cratis.Chronicle.Events.Constraints.IConstraintBuilder builder) =>
            builder.Unique(u => u.On<OrderPlaced>(e => e.OrderId));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ConstraintExpressionLambdaAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}
