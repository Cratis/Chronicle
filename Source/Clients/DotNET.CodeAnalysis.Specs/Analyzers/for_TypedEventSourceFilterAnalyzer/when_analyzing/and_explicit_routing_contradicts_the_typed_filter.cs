// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_TypedEventSourceFilterAnalyzer.when_analyzing;

public class and_explicit_routing_contradicts_the_typed_filter : given.a_typed_event_source_filter_analyzer
{
    const string Usage = """
        [EventSource("orders")]
        [EventStream("placement")]
        public class OrderEventSource : IEventSource { }

        [FromEventSource<OrderEventSource>("placement")]
        {|#0:[EventSourceType("invoices")]|}
        {|#1:[EventStreamType("payment")]|}
        public class OrderReactor { }
        """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.TypedEventSourceFilterAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.ExplicitRoutingContradictsTypedEventSourceFilter, DiagnosticSeverity.Error, "EventSourceTypeAttribute", "OrderEventSource"),
        new ExpectedDiagnostic(DiagnosticIds.ExplicitRoutingContradictsTypedEventSourceFilter, DiagnosticSeverity.Error, "EventStreamTypeAttribute", "OrderEventSource"));

    [Fact] Task should_report_each_contradiction() => _result;
}
