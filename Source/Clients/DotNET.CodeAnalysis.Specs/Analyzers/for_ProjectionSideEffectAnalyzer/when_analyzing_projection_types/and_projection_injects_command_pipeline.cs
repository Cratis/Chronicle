// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ProjectionSideEffectAnalyzer.when_analyzing_projection_types;

public class and_projection_injects_command_pipeline : given.a_projection_side_effect_analyzer
{
    const string Usage = """
    public class ReadModel { }

    public class MyProjection : Cratis.Chronicle.Projections.IProjectionFor<ReadModel>
    {
        public MyProjection({|#0:Cratis.Chronicle.Commands.ICommandPipeline pipeline|}) { }
        public void Define(Cratis.Chronicle.Projections.IProjectionBuilderFor<ReadModel> builder) { }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ProjectionSideEffectAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ProjectionMustNotHaveSideEffects, DiagnosticSeverity.Error, "MyProjection", "ICommandPipeline"));

    [Fact] Task should_report_side_effect_diagnostic() => _result;
}
