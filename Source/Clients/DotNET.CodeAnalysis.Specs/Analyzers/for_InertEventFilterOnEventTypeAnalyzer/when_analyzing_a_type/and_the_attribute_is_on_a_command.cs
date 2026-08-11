// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_InertEventFilterOnEventTypeAnalyzer.when_analyzing_a_type;

/// <summary>
/// One of the two placements the documentation keeps. A command really does contribute this metadata: it tags
/// the events its append produces and, with concurrency, joins the server-side concurrency scope. The same
/// attribute is doing its job here and must stay unreported - the rule keys on the event type, not on the
/// attribute's mere presence.
/// </summary>
public class and_the_attribute_is_on_a_command : given.an_inert_event_filter_on_event_type_analyzer
{
    const string Usage = """
    [EventStreamType("onboarding", concurrency: true)]
    [EventSourceType("account")]
    [Command]
    public record OpenAccount(string Name);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.InertEventFilterOnEventTypeAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_report_nothing() => _result;
}
