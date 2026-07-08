// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ProjectionExpressionLambdaAnalyzer.when_analyzing_projection_expression_lambdas;

public class and_expression_lambda_contains_arithmetic : given.a_projection_expression_lambda_analyzer
{
    const string Usage = """
    public class AnEvent { public int Amount { get; set; } }
    public class ReadModel { public int Total { get; set; } }

    public class MyProjection : Cratis.Chronicle.Projections.IProjectionFor<ReadModel>
    {
        public void Define(Cratis.Chronicle.Projections.IProjectionBuilderFor<ReadModel> builder) =>
            builder.From<AnEvent>(evt => evt
                .Set(x => x.Total)
                .To(e => {|#0:e.Amount * 2|}));
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ProjectionExpressionLambdaAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ProjectionExpressionLambdaMustOnlyAccessMembers, Microsoft.CodeAnalysis.DiagnosticSeverity.Error, "MyProjection"));

    [Fact] Task should_report_arithmetic_diagnostic() => _result;
}
