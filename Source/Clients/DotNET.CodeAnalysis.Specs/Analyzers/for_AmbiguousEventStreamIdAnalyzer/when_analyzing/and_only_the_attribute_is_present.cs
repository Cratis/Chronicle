// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_AmbiguousEventStreamIdAnalyzer.when_analyzing;

public class and_only_the_attribute_is_present : given.an_ambiguous_event_stream_id_analyzer
{
    const string Usage = """
    [EventStreamId("orders")]
    public class PlaceOrder
    {
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.AmbiguousEventStreamIdAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_not_report_any_diagnostic() => _result;
}
