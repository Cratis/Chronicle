// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Tasks;

namespace Cratis.Chronicle.Grains.EventSequences.for_AppendedEventsQueue.when_enqueuing;

public class single_event_with_single_subscriber : Specification
{
    record HandledEvents(Key Partition, IEnumerable<AppendedEvent> Events);
    ITaskFactory _taskFactory;
    IGrainFactory _grainFactory;

    AppendedEventsQueue _queue;
    ObserverKey _observerKey;
    EventType _eventType;
    AppendedEvent _appendedEvent;
    IObserver _observer;
    EventSourceId _eventSourceId;

    List<HandledEvents> _handledEvents = [];

    async Task Establish()
    {
        _taskFactory = Substitute.For<ITaskFactory>();
        _grainFactory = Substitute.For<IGrainFactory>();
        _observerKey = new ObserverKey("Some observer", "Some event store", "Some namespace", "Some event sequence");
        _eventType = new EventType("Some event", 1);
        _appendedEvent = AppendedEvent.EmptyWithEventType(_eventType);
        _eventSourceId = Guid.NewGuid();
        _appendedEvent = _appendedEvent with { Context = EventContext.Empty with { EventSourceId = _eventSourceId } };

        _taskFactory
            .When(_ => _.Run(Arg.Any<Func<Task>>()))
            .Do(callInfo => callInfo.Arg<Func<Task>>()());

        _observer = Substitute.For<IObserver>();
        _observer
            .When(_ => _.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>()))
            .Do(callInfo =>
            {
                var key = callInfo.Arg<Key>();
                var events = callInfo.Arg<IEnumerable<AppendedEvent>>();
                _handledEvents.Add(new(key, events));
            });
        _grainFactory.GetGrain<IObserver>(_observerKey).Returns(_observer);

        _queue = new AppendedEventsQueue(_taskFactory, _grainFactory);
        await _queue.Subscribe(_observerKey, [_eventType]);
    }

    async Task Because()
    {
        await _queue.Enqueue([_appendedEvent]);
        await _queue.AwaitQueueDepletion();
    }

    [Fact] void should_call_handle_on_observer_once() => _handledEvents.Count.ShouldEqual(1);
    [Fact] void should_call_handle_on_observer_with_correct_event_source_id() => _handledEvents[0].Partition.Value.ShouldEqual(_eventSourceId.Value);
}
