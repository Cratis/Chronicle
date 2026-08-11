// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_InertEventFilterOnEventTypeAnalyzer.when_analyzing_a_type;

/// <summary>
/// The other placement the documentation keeps. A reactor's subscription carries these values and the kernel
/// narrows the observed stream by them, so the attribute is load-bearing here - this is the very reader whose
/// absence for event types makes the event type placement inert.
/// </summary>
public class and_the_attribute_is_on_an_observer : given.an_inert_event_filter_on_event_type_analyzer
{
    const string Usage = """
    [EventStreamType("onboarding")]
    [EventSourceType("account")]
    public class TheReactor : Cratis.Chronicle.Reactors.IReactor
    {
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.InertEventFilterOnEventTypeAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_report_nothing() => _result;
}
