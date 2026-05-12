// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReducerMultipleEventStoresAnalyzer.when_analyzing_reducer_types;

public class and_event_types_have_different_event_stores : given.a_reducer_multiple_event_stores_analyzer
{
    const string Usage = """
    [EventType]
    [EventStore("event-store-one")]
    public class EventFromFirstStore { }

    [EventType]
    [EventStore("event-store-two")]
    public class EventFromSecondStore { }

    {|#0:public class ReducerWithMixedEventStores : Cratis.Chronicle.Reducers.IReducer
    {
        public object Reduce(EventFromFirstStore @event, object? current) => new();
        public object Reduce(EventFromSecondStore @event, object? current) => new();
    }|}
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReducerMultipleEventStoresAnalyzer>.VerifyAnalyzer(CreateSource(Usage), new ExpectedDiagnostic(DiagnosticIds.ReducerEventTypesMustBeFromSameEventStore, DiagnosticSeverity.Error, "ReducerWithMixedEventStores", "event-store-one", "event-store-two"));

    [Fact] Task should_report_multiple_event_stores_diagnostic() => _result;
}
