// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorMultipleEventStoresAnalyzer.when_analyzing_reactor_types;

public class and_all_event_types_have_the_same_event_store : given.a_reactor_multiple_event_stores_analyzer
{
    const string Usage = """
    [EventType]
    [EventStore("event-store-one")]
    public class EventFromFirstStore { }

    [EventType]
    [EventStore("event-store-one")]
    public class AnotherEventFromSameStore { }

    public class ValidReactor : Cratis.Chronicle.Reactors.IReactor
    {
        public void Handle(EventFromFirstStore @event) { }
        public void Handle(AnotherEventFromSameStore @event) { }
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMultipleEventStoresAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
