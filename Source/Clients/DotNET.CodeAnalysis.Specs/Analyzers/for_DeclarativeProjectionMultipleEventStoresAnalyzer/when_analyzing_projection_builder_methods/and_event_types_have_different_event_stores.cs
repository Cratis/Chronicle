// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DeclarativeProjectionMultipleEventStoresAnalyzer.when_analyzing_projection_builder_methods;

public class and_event_types_have_different_event_stores : given.a_declarative_projection_multiple_event_stores_analyzer
{
    const string Usage = """
    [EventType]
    [EventStore("event-store-one")]
    public class EventFromFirstStore { }

    [EventType]
    [EventStore("event-store-two")]
    public class EventFromSecondStore { }

    public class Projection
    {
        readonly Cratis.Chronicle.Projections.IProjectionBuilderFor<Projection> _builder;

        public Projection(Cratis.Chronicle.Projections.IProjectionBuilderFor<Projection> builder)
        {
            _builder = builder;
        }

        public void Build()
        {
            _builder.From<EventFromFirstStore>();
            {|#0:_builder.From<EventFromSecondStore>()|};
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.DeclarativeProjectionMultipleEventStoresAnalyzer>.VerifyAnalyzer(CreateSource(Usage), new ExpectedDiagnostic(DiagnosticIds.DeclarativeProjectionEventTypesMustBeFromSameEventStore, DiagnosticSeverity.Error, "event-store-one", "event-store-two"));

    [Fact] Task should_report_multiple_event_stores_diagnostic() => _result;
}
