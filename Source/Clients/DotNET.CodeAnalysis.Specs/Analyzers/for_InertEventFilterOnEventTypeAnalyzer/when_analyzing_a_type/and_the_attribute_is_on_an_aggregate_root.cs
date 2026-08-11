// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_InertEventFilterOnEventTypeAnalyzer.when_analyzing_a_type;

/// <summary>
/// The third live placement, and the one the rule's first wording denied existed. Arc reads the event stream type
/// off the aggregate root type and falls back to the aggregate's own type name when the attribute is absent, so
/// the value becomes the stream type of every event that aggregate appends. Reporting it here would invite the
/// author to delete the attribute, which does not error - it silently moves every subsequent append to a
/// different stream type, which is the exact damage this rule exists to prevent.
/// </summary>
/// <remarks>
/// Only <c>[EventStreamType]</c> is declared. The event source type is passed to the aggregate root factory as a
/// parameter and never read from an attribute, so pinning a skip for it here would pin a claim that is not true.
/// </remarks>
public class and_the_attribute_is_on_an_aggregate_root : given.an_inert_event_filter_on_event_type_analyzer
{
    const string Usage = """
    [EventStreamType("onboarding")]
    [Cratis.Chronicle.Events.EventTypeAttribute]
    public class TheAggregate : Cratis.Arc.Chronicle.Aggregates.IAggregateRoot
    {
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.InertEventFilterOnEventTypeAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_report_nothing() => _result;
}
