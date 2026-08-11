// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_InertEventFilterOnEventTypeAnalyzer.when_analyzing_a_type;

/// <summary>
/// The rule is scoped to event types, and this is the only spec that measures that scoping on its own. The type
/// carries neither <c>[EventType]</c> nor any of the roles that read the attribute, so every other negative in
/// this folder would stay green with the event type check deleted - each of them is skipped for its role first.
/// This one is not, and reddens the moment the rule stops asking whether the type is an event type at all.
/// </summary>
public class and_the_type_is_not_an_event_type : given.an_inert_event_filter_on_event_type_analyzer
{
    const string Usage = """
    [EventStreamType("onboarding")]
    [EventSourceType("account")]
    public record AccountSummary(string Name);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.InertEventFilterOnEventTypeAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_report_nothing() => _result;
}
