// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerMultipleEventStoresAnalyzer.when_analyzing_reducer_types;

public class and_all_event_types_have_same_event_store_from_assembly_attribute : given.a_reducer_multiple_event_stores_analyzer
{
    const string AssemblyAttributes = """[assembly: Cratis.Chronicle.Events.EventStore("event-store-one")]""";

    const string Usage = """
    [EventType]
    public class EventFromFirstStore { }

    [EventType]
    public class AnotherEventFromSameStore { }

    public class ValidReducer : Cratis.Chronicle.Reducers.IReducer
    {
        public EventFromFirstStore Reduce(EventFromFirstStore @event, EventFromFirstStore? current) => @event;
        public AnotherEventFromSameStore Reduce(AnotherEventFromSameStore @event, AnotherEventFromSameStore? current) => @event;
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMultipleEventStoresAnalyzer>.VerifyAnalyzer(CreateSource(Usage, AssemblyAttributes));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
