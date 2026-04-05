// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_DeclarativeProjectionMultipleEventStoresAnalyzer.when_analyzing_projection_builder_methods;

public class and_all_event_types_have_same_event_store_from_assembly_attribute : given.a_declarative_projection_multiple_event_stores_analyzer
{
    const string AssemblyAttributes = """[assembly: Cratis.Chronicle.Events.EventStore("event-store-one")]""";

    const string Usage = """
    [EventType]
    public class EventFromFirstStore { }

    [EventType]
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

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.DeclarativeProjectionMultipleEventStoresAnalyzer>.VerifyAnalyzer(CreateSource(Usage, AssemblyAttributes));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
