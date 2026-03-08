// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

public abstract class a_single_subscriber : all_dependencies
{
    protected ObserverKey _observerKey;
    protected IObserver _observer;
    protected ConcurrentDictionary<Key, List<HandledEvents>> _handledEventsPerPartition = [];
    protected AppendedEventsQueue _queue;

    async Task Establish()
    {
        _observerKey = new ObserverKey("Some observer", "Some event store", "Some namespace", "Some event sequence");
        _observer = Substitute.For<IObserver>();
        _observer
            .When(_ => _.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>()))
            .Do(callInfo =>
            {
                var key = callInfo.Arg<Key>();
                var events = callInfo.Arg<IEnumerable<AppendedEvent>>();
                if (!_handledEventsPerPartition.TryGetValue(key, out var handledEvents))
                {
                    handledEvents = [];
                    _handledEventsPerPartition.TryAdd(key, handledEvents);
                }
                handledEvents.Add(new(key, events));
            });
        _grainFactory.GetGrain<IObserver>(_observerKey).Returns(_observer);

        _queue = new AppendedEventsQueue(
            _taskFactory,
            _grainFactory,
            Substitute.For<IMeter<AppendedEventsQueue>>(),
            Substitute.For<ILogger<AppendedEventsQueue>>());
        await _queue.Subscribe(_observerKey, EventTypes);
    }

    protected abstract IEnumerable<EventType> EventTypes { get; }
}
