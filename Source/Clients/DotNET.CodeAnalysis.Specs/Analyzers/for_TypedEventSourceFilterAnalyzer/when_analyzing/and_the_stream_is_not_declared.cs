// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_TypedEventSourceFilterAnalyzer.when_analyzing;

public class and_the_stream_is_not_declared : given.a_typed_event_source_filter_analyzer
{
    const string Usage = """
        [EventSource("orders")]
        [EventStream("placement")]
        public class OrderEventSource : IEventSource { }

        {|#0:[FromEventSource<OrderEventSource>("shipping")]|}
        public class ShippingReactor { }
        """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.TypedEventSourceFilterAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.UnknownEventStreamOnTypedEventSourceFilter, DiagnosticSeverity.Error, "OrderEventSource", "shipping"));

    [Fact] Task should_report_an_unknown_stream() => _result;
}
