// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation;

namespace Cratis.Chronicle.Grains.EventSequences.for_AppendedEventsQueue.given;

public abstract class two_subscribers : all_dependencies
{
    protected AppendedEventsQueue _queue;
    protected ObserverKey _firstObserverKey;
    protected ObserverKey _secondObserverKey;
    protected IObserver _firstObserver;
    protected IObserver _secondObserver;
    protected List<HandledEvents> _firstObserverHandledEvents = [];
    protected List<HandledEvents> _secondObserverHandledEvents = [];

    async Task Establish()
    {
        _queue = new AppendedEventsQueue(_taskFactory, _grainFactory);
        _firstObserverKey = new ObserverKey("First observer", "Some event store", "Some namespace", "Some event sequence");
        _firstObserver = Substitute.For<IObserver>();
        _firstObserver
            .When(_ => _.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>()))
            .Do(callInfo =>
            {
                var key = callInfo.Arg<Key>();
                var events = callInfo.Arg<IEnumerable<AppendedEvent>>();
                _firstObserverHandledEvents.Add(new(key, events));
            });
        _grainFactory.GetGrain<IObserver>(_firstObserverKey).Returns(_firstObserver);
        await _queue.Subscribe(_firstObserverKey, EventTypes);

        _secondObserverKey = new ObserverKey("Second observer", "Some event store", "Some namespace", "Some event sequence");
        _secondObserver = Substitute.For<IObserver>();
        _secondObserver
            .When(_ => _.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>()))
            .Do(callInfo =>
            {
                var key = callInfo.Arg<Key>();
                var events = callInfo.Arg<IEnumerable<AppendedEvent>>();
                _secondObserverHandledEvents.Add(new(key, events));
            });
        _grainFactory.GetGrain<IObserver>(_secondObserverKey).Returns(_secondObserver);
        await _queue.Subscribe(_secondObserverKey, EventTypes);
    }

    protected abstract IEnumerable<EventType> EventTypes { get; }
}
