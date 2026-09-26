// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorMethodAnalyzer.when_analyzing_reactor_methods;

public class and_return_types_match_existing_runtime_shapes : given.a_reactor_method_analyzer
{
    const string Usage = """
    [EventType]
    public class KnownEvent
    {
    }

    [EventType]
    public class OutboundEvent
    {
    }

    public class DerivedTask : Task
    {
        public DerivedTask() : base(() => { }) { }
    }

    public class Reactor : Cratis.Chronicle.Reactors.IReactor
    {
        public void NoResult(KnownEvent @event) { }
        public Task NoAsyncResult(KnownEvent @event) => Task.CompletedTask;
        public DerivedTask DerivedAsyncResult(KnownEvent @event) => new();
        public OutboundEvent Event(KnownEvent @event) => new();
        public Cratis.Chronicle.EventSequences.EventForEventSourceId TargetedEvent(KnownEvent @event) => new();
        public System.Collections.Generic.IEnumerable<OutboundEvent> Events(KnownEvent @event) => new OutboundEvent[] { new() };
        public System.Collections.Generic.IEnumerable<Cratis.Chronicle.EventSequences.EventForEventSourceId> TargetedEvents(KnownEvent @event) => new Cratis.Chronicle.EventSequences.EventForEventSourceId[] { new() };
        public System.Collections.Generic.IEnumerable<object> MixedEvents(KnownEvent @event) => new object[] { new OutboundEvent(), new Cratis.Chronicle.EventSequences.EventForEventSourceId() };
        public Task<OutboundEvent> AsyncEvent(KnownEvent @event) => Task.FromResult(new OutboundEvent());
        public Task<string> CustomSideEffect(KnownEvent @event) => Task.FromResult("processed");
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
