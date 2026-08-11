// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_InertEventFilterOnEventTypeAnalyzer.when_analyzing_a_type;

/// <summary>
/// The second observer kind, and a separate reader: the client builds a reducer's subscription from the same two
/// attributes read off the reducer type, so they are live here for the same reason they are live on a reactor and
/// must be skipped for the same reason. Kept apart from the reactor's spec so that losing one of the two role
/// checks cannot be absorbed by the other.
/// </summary>
public class and_the_attribute_is_on_a_reducer : given.an_inert_event_filter_on_event_type_analyzer
{
    const string Usage = """
    [EventStreamType("onboarding")]
    [EventSourceType("account")]
    [Cratis.Chronicle.Events.EventTypeAttribute]
    public class TheReducer : Cratis.Chronicle.Reducers.IReducer
    {
    }
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.InertEventFilterOnEventTypeAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_report_nothing() => _result;
}
