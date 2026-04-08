// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerMultipleEventStoresAnalyzer.when_analyzing_reducer_types;

public class and_all_event_types_have_the_same_event_store : given.a_reducer_multiple_event_stores_analyzer
{
    const string Usage = """
    [EventType]
    [EventStore("event-store-one")]
    public class EventFromFirstStore { }

    [EventType]
    [EventStore("event-store-one")]
    public class AnotherEventFromSameStore { }

    public class ValidReducer : Cratis.Chronicle.Reducers.IReducer
    {
        public object Reduce(EventFromFirstStore @event, object? current) => new();
        public object Reduce(AnotherEventFromSameStore @event, object? current) => new();
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMultipleEventStoresAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
