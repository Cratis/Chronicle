// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Observation.for_DefinitionEvolutionPlan.when_planning_added_reducer_event_types;

public class and_added_events_form_a_suffix : Specification
{
    static readonly EventType _existingEventType = new("existing", 1);
    static readonly EventType _addedEventType = new("added", 1);
    IEventSequenceStorage _eventSequence;
    IEventCursor _cursor;
    DefinitionEvolutionPlan _result;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequenceStorage>();
        _cursor = Substitute.For<IEventCursor>();
        _eventSequence.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)2UL);
        _eventSequence.GetFromSequenceNumber(EventSequenceNumber.First, eventTypes: Arg.Any<IEnumerable<EventType>>()).Returns(_cursor);
        _cursor.MoveNext().Returns(true, false);
        _cursor.Current.Returns([
            Event(_existingEventType, 0),
            Event(_addedEventType, 1),
            Event(_addedEventType, 2)
        ]);
    }

    async Task Because() => _result = await DefinitionEvolutionPlan.ForAddedReducerEventTypes(
        _eventSequence,
        [_addedEventType],
        [_existingEventType],
        true);

    [Fact] void should_choose_partial_replay() => _result.Operation.ShouldEqual(DefinitionEvolutionOperation.PartialReplay);
    [Fact] void should_only_replay_the_added_event_type() => _result.AffectedEventTypes.ShouldContainOnly(_addedEventType);

    static AppendedEvent Event(EventType eventType, ulong sequenceNumber) =>
        new(EventContext.Empty with { EventSourceId = "source", EventType = eventType, SequenceNumber = sequenceNumber }, new ExpandoObject());
}
