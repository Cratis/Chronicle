// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_InertEventFilterOnEventTypeAnalyzer.when_analyzing_a_type;

/// <summary>
/// An event type is spelled two ways - the client's <c>Cratis.Chronicle.Events.EventTypeAttribute</c> and the
/// kernel's <c>Cratis.Chronicle.Concepts.Events.EventTypeAttribute</c> - and the shared check accepts both.
/// Nothing pinned the kernel spelling here, so narrowing the check to the client one left every spec in this
/// folder green while the rule went silent on half the event types it exists for.
/// </summary>
public class and_the_event_type_carries_the_kernel_attribute : given.an_inert_event_filter_on_event_type_analyzer
{
    const string Usage = """
    {|#0:[EventStreamType("onboarding")]|}
    [Cratis.Chronicle.Concepts.Events.EventTypeAttribute]
    public record AccountOpened(string Name);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.InertEventFilterOnEventTypeAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.InertEventFilterOnEventType, DiagnosticSeverity.Warning, "EventStreamTypeAttribute", "AccountOpened"));

    [Fact] Task should_report_that_the_placement_does_nothing() => _result;
}
