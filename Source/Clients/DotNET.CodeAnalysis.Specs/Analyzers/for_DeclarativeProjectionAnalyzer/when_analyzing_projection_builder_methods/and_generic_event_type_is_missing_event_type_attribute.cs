// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DeclarativeProjectionAnalyzer.when_analyzing_projection_builder_methods;

public class and_generic_event_type_is_missing_event_type_attribute : given.a_declarative_projection_analyzer
{
    const string Usage = """
    public class MissingEvent
    {
    }

    public class Projection
    {
        readonly Cratis.Chronicle.Projections.IProjectionBuilderFor<Projection> _builder;

        public Projection(Cratis.Chronicle.Projections.IProjectionBuilderFor<Projection> builder)
        {
            _builder = builder;
        }

        public void Build()
        {
            {|#0:_builder.From<MissingEvent>()|};
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.DeclarativeProjectionAnalyzer>.VerifyAnalyzer(CreateSource(Usage), new ExpectedDiagnostic(DiagnosticIds.DeclarativeProjectionEventTypeMustHaveAttribute, DiagnosticSeverity.Error, "MissingEvent"));

    [Fact] Task should_report_event_type_attribute_diagnostic() => _result;
}
