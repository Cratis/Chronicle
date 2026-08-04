// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_InertEventFilterOnProjectionAnalyzer.when_analyzing_an_observer;

/// <summary>
/// A projection observes every event of the types its definition declares. There is no field for a filter
/// anywhere in a projection's definition, so nothing is transmitted and nothing is applied - the attribute reads
/// as a fence and is not one.
/// </summary>
public class and_it_is_a_projection : given.an_inert_event_filter_analyzer
{
    const string Usage = """
    [Cratis.Chronicle.Events.EventTypeAttribute]
    public class SomethingHappened
    {
    }

    {|#0:[EventStreamType("the-stream")]|}
    [FromEvent<SomethingHappened>]
    public record Summary(Guid Id);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.InertEventFilterOnProjectionAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.InertEventFilterOnProjection, DiagnosticSeverity.Warning, "EventStreamTypeAttribute", "Summary"));

    [Fact] Task should_report_that_the_filter_does_nothing() => _result;
}
