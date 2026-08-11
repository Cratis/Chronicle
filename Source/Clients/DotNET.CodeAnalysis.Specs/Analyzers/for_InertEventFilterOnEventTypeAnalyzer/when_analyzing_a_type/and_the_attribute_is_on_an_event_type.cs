// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_InertEventFilterOnEventTypeAnalyzer.when_analyzing_a_type;

/// <summary>
/// An append resolves its event source type and event stream type from the arguments it is given, never from the
/// CLR type of the event being appended. Nothing reads either attribute off an event type, so the declaration
/// tags no appended event and narrows no observer - it reads as stream identity and is not one. Both attributes
/// are declared here because both make the claim and both are equally inert.
/// </summary>
public class and_the_attribute_is_on_an_event_type : given.an_inert_event_filter_on_event_type_analyzer
{
    const string Usage = """
    {|#0:[EventStreamType("onboarding")]|}
    {|#1:[EventSourceType("account")]|}
    [Cratis.Chronicle.Events.EventTypeAttribute]
    public record AccountOpened(string Name);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.InertEventFilterOnEventTypeAnalyzer>.VerifyAnalyzer(
        CreateSource(Usage),
        new ExpectedDiagnostic(DiagnosticIds.InertEventFilterOnEventType, DiagnosticSeverity.Warning, "EventStreamTypeAttribute", "AccountOpened"),
        new ExpectedDiagnostic(DiagnosticIds.InertEventFilterOnEventType, DiagnosticSeverity.Warning, "EventSourceTypeAttribute", "AccountOpened"));

    [Fact] Task should_report_that_both_placements_do_nothing() => _result;
}
