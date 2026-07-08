// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_AmbiguousEventStreamIdAnalyzer.when_analyzing;

public class and_both_interface_and_non_null_attribute_are_present : given.an_ambiguous_event_stream_id_analyzer
{
    const string Usage = """
    {|#0:[EventStreamId("orders")]|}
    public class PlaceOrder : ICanProvideEventStreamId
    {
        public string GetEventStreamId() => "orders";
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.AmbiguousEventStreamIdAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.AmbiguousEventStreamId, DiagnosticSeverity.Error, "PlaceOrder"));

    [Fact] Task should_report_the_diagnostic() => _result;
}
