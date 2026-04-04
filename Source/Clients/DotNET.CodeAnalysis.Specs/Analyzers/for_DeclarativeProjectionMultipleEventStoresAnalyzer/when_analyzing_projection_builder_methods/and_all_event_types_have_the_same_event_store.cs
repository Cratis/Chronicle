// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DeclarativeProjectionMultipleEventStoresAnalyzer.when_analyzing_projection_builder_methods;

public class and_all_event_types_have_the_same_event_store : given.a_declarative_projection_multiple_event_stores_analyzer
{
    const string Usage = """
    [EventType]
    [EventStore("event-store-one")]
    public class EventFromFirstStore { }

    [EventType]
    [EventStore("event-store-one")]
    public class AnotherEventFromSameStore { }

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
            _builder.From<AnotherEventFromSameStore>();
        }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.DeclarativeProjectionMultipleEventStoresAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
