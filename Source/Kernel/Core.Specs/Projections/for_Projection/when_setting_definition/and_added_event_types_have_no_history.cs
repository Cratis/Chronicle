// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.when_setting_definition;

public class and_added_event_types_have_no_history : given.a_projection_adding_historical_event_mappings
{
    void Establish() => _eventSequence.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>()).Returns(EventSequenceNumber.Unavailable);

    async Task Because() => await _grain.SetDefinition(_candidate);

    [Fact] void should_not_schedule_a_replay() => _silo.TimerRegistry.NumberOfActiveTimers.ShouldEqual(0);
    [Fact] void should_not_call_the_observer() => _observer.ReceivedCalls().ShouldBeEmpty();
    [Fact] void should_apply_the_definition() => _stateStorage.State.ShouldBeSame(_candidate);
}
