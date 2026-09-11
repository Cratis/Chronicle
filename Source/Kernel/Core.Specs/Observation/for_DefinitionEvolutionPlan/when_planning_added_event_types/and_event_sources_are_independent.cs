// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Observation.for_DefinitionEvolutionPlan.when_planning_added_event_types;

public class and_event_sources_are_independent : Specification
{
    IEventSequenceStorage _eventSequence;
    IEventCursor _cursor;
    DefinitionEvolutionPlan _result;

    void Establish()
    {
        _eventSequence = Substitute.For<IEventSequenceStorage>();
        _cursor = Substitute.For<IEventCursor>();
        _eventSequence.GetTailSequenceNumber(Arg.Any<IEnumerable<EventType>>()).Returns((EventSequenceNumber)42UL);
        _eventSequence.GetFromSequenceNumber(EventSequenceNumber.First, eventTypes: Arg.Any<IEnumerable<EventType>>()).Returns(_cursor);
        _cursor.MoveNext().Returns(true, false);
        _cursor.Current.Returns([
            new(EventContext.Empty with { EventSourceId = "first" }, new ExpandoObject()),
            new(EventContext.Empty with { EventSourceId = "second" }, new ExpandoObject()),
            new(EventContext.Empty with { EventSourceId = "first" }, new ExpandoObject())
        ]);
    }

    async Task Because() => _result = await DefinitionEvolutionPlan.ForAddedEventTypes(
        _eventSequence,
        [new EventType("new-event", 1)],
        true);

    [Fact] void should_choose_partial_replay() => _result.Operation.ShouldEqual(DefinitionEvolutionOperation.PartialReplay);
    [Fact] void should_only_include_affected_event_sources() => _result.AffectedEventSources.ShouldContainOnly<EventSourceId>("first", "second");
    [Fact] void should_only_replay_the_added_event_type() => _result.AffectedEventTypes.ShouldContainOnly(new EventType("new-event", 1));
}
