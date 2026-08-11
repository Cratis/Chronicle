// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.CodeAnalysis.Specs.Testing;

namespace Cratis.Chronicle.CodeAnalysis.Specs.Analyzers.for_InertEventFilterOnEventTypeAnalyzer.when_analyzing_a_type;

/// <summary>
/// A command really does contribute this metadata: it tags the events its append produces and, with concurrency,
/// joins the server-side concurrency scope. The command here also carries <c>[EventType]</c>, which is what makes
/// this spec measure anything: without it the type would be skipped for having no event type at all, and the
/// command role would be scenery the analyzer never reads. Arc reads both attributes off the command type, so
/// reporting them would be a false positive - and telling the author to "move it to the command that appends the
/// event" when the type is that command is advice with nowhere to go.
/// </summary>
public class and_the_attribute_is_on_a_command : given.an_inert_event_filter_on_event_type_analyzer
{
    const string Usage = """
    [EventStreamType("onboarding", concurrency: true)]
    [EventSourceType("account")]
    [Cratis.Chronicle.Events.EventTypeAttribute]
    [Command]
    public record OpenAccount(string Name);
    """;

    Task _result;

    void Because() => _result = AnalyzerVerifier<CodeAnalysis.Analyzers.InertEventFilterOnEventTypeAnalyzer>.VerifyAnalyzer(CreateSource(Usage));

    [Fact] Task should_report_nothing() => _result;
}
