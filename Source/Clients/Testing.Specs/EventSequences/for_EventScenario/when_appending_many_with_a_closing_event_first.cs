// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Testing.EventSequences.for_EventScenario;

public class when_appending_many_with_a_closing_event_first : given.a_scenario_with_closing_events
{
    AppendManyResult _result;

    async Task Because() => _result = await _scenario.EventLog.AppendMany("source", [new BooksClosed("April"), new Booked("covered")], "transactions", "April");

    [Fact] void should_refuse_the_batch() => _result.ShouldBeFailed();
    [Fact] void should_name_the_covered_event() => _result.ConstraintViolations.Any(violation => violation.EventTypeId == _bookedType).ShouldBeTrue();
    [Fact] async Task should_not_close_any_scope_for_a_rejected_batch() => (await _scenario.EventSequence.GetClosedStreams()).ShouldBeEmpty();
}
