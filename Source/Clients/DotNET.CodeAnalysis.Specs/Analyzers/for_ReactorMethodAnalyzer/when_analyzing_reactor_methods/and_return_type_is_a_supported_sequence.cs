// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_ReactorMethodAnalyzer.when_analyzing_reactor_methods;

public class and_return_type_is_a_supported_sequence : given.a_reactor_method_analyzer
{
    const string Usage = """
    [EventType]
    public class KnownEvent { }

    [EventType]
    public class OutboundEvent { }

    public class Reactor : Cratis.Chronicle.Reactors.IReactor
    {
        public OutboundEvent[] Array(KnownEvent @event) => new OutboundEvent[] { new() };
        public System.Collections.Generic.List<OutboundEvent> List(KnownEvent @event) => new() { new() };
        public System.Collections.Generic.IReadOnlyList<OutboundEvent> ReadOnlyList(KnownEvent @event) => new OutboundEvent[] { new() };
        public object[] MixedArray(KnownEvent @event) => new object[] { new OutboundEvent() };
        public System.Collections.Generic.List<object> MixedList(KnownEvent @event) => new() { new OutboundEvent() };
        public Cratis.Chronicle.EventSequences.EventForEventSourceId[] TargetedArray(KnownEvent @event) => new Cratis.Chronicle.EventSequences.EventForEventSourceId[] { new() };
        public System.Collections.Generic.List<Cratis.Chronicle.EventSequences.EventForEventSourceId> TargetedList(KnownEvent @event) => new() { new() };
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.ReactorMethodAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_unsupported_returns() => _result;
}
