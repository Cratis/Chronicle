// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ProjectionSideEffectAnalyzer.when_analyzing_projection_types;

public class and_projection_has_no_side_effect_injections : given.a_projection_side_effect_analyzer
{
    const string Usage = """
    public class ReadModel { }
    public interface ISomeService { }

    public class MyProjection : Cratis.Chronicle.Projections.IProjectionFor<ReadModel>
    {
        public MyProjection(ISomeService service) { }
        public void Define(Cratis.Chronicle.Projections.IProjectionBuilderFor<ReadModel> builder) { }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ProjectionSideEffectAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostics() => _result;
}
