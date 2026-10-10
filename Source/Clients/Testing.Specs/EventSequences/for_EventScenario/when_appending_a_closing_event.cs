// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_appending_a_closing_event : given.a_scenario_with_closing_events
{
    AppendResult _closing;
    AppendResult _covered;
    AppendResult _otherSource;

    async Task Because()
    {
        _closing = await _scenario.EventLog.Append("source", new BooksClosed("April"));
        _covered = await _scenario.EventLog.Append("source", new Booked("covered"), "transactions", "April");
        _otherSource = await _scenario.EventLog.Append("other-source", new Booked("open"), "transactions", "April");
    }

    [Fact] void should_accept_the_closing_fact() => _closing.ShouldBeSuccessful();
    [Fact] void should_refuse_the_covered_event() => _covered.ShouldHaveConstraintViolation("closed-stream");
    [Fact] void should_leave_other_sources_open() => _otherSource.ShouldBeSuccessful();
    [Fact] async Task should_report_the_closing_owner() => (await _scenario.EventSequence.GetClosedStreams()).Single().ClosedBy!.Value.ShouldEqual("close-books");
    [Fact] async Task should_report_event_origin() => (await _scenario.EventSequence.GetClosedStreams()).Single().Origin.ShouldEqual(ClosedStreamOrigin.ClosingEvent);
}
