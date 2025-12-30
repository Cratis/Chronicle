// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Reducers.for_Reducers.when_getting_snapshots_by_id;

public class with_no_events : given.all_dependencies
{
    class MyReadModel
    {
        public string Name { get; set; }
    }

    ReadModelKey _key;
    IReducerHandler _handler;
    IEventSequence _eventSequence;
    ImmutableList<AppendedEvent> _events;
    IEnumerable<ReducerSnapshot<MyReadModel>> _result;

    void Establish()
    {
        _key = "test-key";

        _handler = Substitute.For<IReducerHandler>();
        _handler.ReadModelType.Returns(typeof(MyReadModel));
        _handler.EventSequenceId.Returns(new EventSequenceId("test-sequence"));
        _handler.EventTypes.Returns([new EventType("test-event", 1)]);

        _handlersByModelType[typeof(MyReadModel)] = _handler;

        _eventSequence = Substitute.For<IEventSequence>();
        _eventStore.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequence);

        _events = [];
        _eventSequence.GetForEventSourceIdAndEventTypes(
            Arg.Any<EventSourceId>(),
            Arg.Any<IEnumerable<EventType>>()).Returns(_events);
    }

    async Task Because() => _result = await _reducers.GetSnapshotsById<MyReadModel>(_key);

    [Fact] void should_get_event_sequence() => _eventStore.Received(1).GetEventSequence(_handler.EventSequenceId);
    [Fact] void should_get_events_for_event_source() => _eventSequence.Received(1).GetForEventSourceIdAndEventTypes(_key, _handler.EventTypes);
    [Fact] void should_return_empty_collection() => _result.ShouldBeEmpty();
}
