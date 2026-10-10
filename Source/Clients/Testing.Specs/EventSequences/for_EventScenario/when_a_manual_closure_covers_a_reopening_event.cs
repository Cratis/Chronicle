// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_a_manual_closure_covers_a_reopening_event : given.a_scenario_with_closing_events
{
    AppendResult _result;

    async Task Establish()
    {
        (await _scenario.EventLog.Append("source", new BooksClosed("April"))).ShouldBeSuccessful();
        (await _scenario.EventSequence.CompleteStream(new ClosedStreamScope(EventSourceId: "source", EventStreamId: "April"))).IsSuccess.ShouldBeTrue();
    }

    async Task Because() => _result = await _scenario.EventLog.Append("source", new BooksReopened("April"), "transactions", "April");

    [Fact] void should_not_exempt_the_manual_owner() => _result.ShouldHaveConstraintViolation("closed-stream");
    [Fact] async Task should_preserve_both_owners() => (await _scenario.EventSequence.GetClosedStreams()).Count.ShouldEqual(2);
}
