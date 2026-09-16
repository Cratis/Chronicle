// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Observation.for_DefinitionEvolutionPlan.when_planning_added_event_types;

public class and_no_historical_events_exist : Specification
{
    IEventSequenceStorage _eventSequence;
    DefinitionEvolutionPlan _result;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequenceStorage>();
        _eventSequence.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>()).Returns(EventSequenceNumber.Unavailable);
    }

    async Task Because() => _result = await DefinitionEvolutionPlan.ForAddedEventTypes(
        _eventSequence,
        [new EventType("new-event", 1)],
        true);

    [Fact] void should_need_no_action() => _result.Operation.ShouldEqual(DefinitionEvolutionOperation.NoAction);
    [Fact] void should_have_no_affected_event_sources() => _result.AffectedEventSources.ShouldBeEmpty();
}
