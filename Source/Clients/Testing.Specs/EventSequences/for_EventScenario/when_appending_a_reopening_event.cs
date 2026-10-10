// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_appending_a_reopening_event : given.a_scenario_with_closing_events
{
    AppendResult _reopened;
    AppendResult _covered;

    async Task Establish() => (await _scenario.EventLog.Append("source", new BooksClosed("April"))).ShouldBeSuccessful();

    async Task Because()
    {
        _reopened = await _scenario.EventLog.Append("source", new BooksReopened("April"), "transactions", "April");
        _covered = await _scenario.EventLog.Append("source", new Booked("open"), "transactions", "April");
    }

    [Fact] void should_accept_the_reopening_fact_in_its_closed_scope() => _reopened.ShouldBeSuccessful();
    [Fact] void should_accept_the_next_covered_event() => _covered.ShouldBeSuccessful();
    [Fact] async Task should_remove_its_owned_closure() => (await _scenario.EventSequence.GetClosedStreams()).ShouldBeEmpty();
}
