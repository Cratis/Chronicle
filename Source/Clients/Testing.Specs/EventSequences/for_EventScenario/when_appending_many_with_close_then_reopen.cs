// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_appending_many_with_close_then_reopen : given.a_scenario_with_closing_events
{
    AppendManyResult _result;

    async Task Because() => _result = await _scenario.EventLog.AppendMany("source", [new BooksClosed("April"), new BooksReopened("April"), new Booked("open")], "transactions", "April");

    [Fact] void should_accept_the_batch_in_event_order() => _result.ShouldBeSuccessful();
    [Fact] async Task should_keep_the_scope_open() => (await _scenario.EventSequence.GetClosedStreams()).ShouldBeEmpty();
}
