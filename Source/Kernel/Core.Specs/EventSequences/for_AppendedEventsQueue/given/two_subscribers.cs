// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

public class two_subscribers : all_dependencies
{
    protected ObserverKey _firstObserverKey;
    protected ObserverKey _secondObserverKey;
    protected IObserver _firstObserver;
    protected IObserver _secondObserver;
    protected ConcurrentDictionary<Key, List<HandledEvents>> _firstObserverHandledEventsPerPartition = [];
    protected ConcurrentDictionary<Key, List<HandledEvents>> _secondObserverHandledEventsPerPartition = [];
    protected AppendedEventsQueue _queue;

    void Establish()
    {
        _queue = new AppendedEventsQueue(
            _taskFactory,
            _grainFactory,
            Substitute.For<IMeter<AppendedEventsQueue>>(),
            Substitute.For<ILogger<AppendedEventsQueue>>());
        _firstObserverKey = new ObserverKey("First observer", "Some event store", "Some namespace", "Some event sequence");
        _firstObserver = Substitute.For<IObserver>();
        _firstObserver
            .When(_ => _.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>()))
            .Do(callInfo =>
            {
                var key = callInfo.Arg<Key>();
                var events = callInfo.Arg<IEnumerable<AppendedEvent>>();
                if (!_firstObserverHandledEventsPerPartition.TryGetValue(key, out var handledEvents))
                {
                    handledEvents = [];
                    _firstObserverHandledEventsPerPartition.TryAdd(key, handledEvents);
                }
                handledEvents.Add(new(key, events));
            });
        _grainFactory.GetGrain<IObserver>(_firstObserverKey).Returns(_firstObserver);

        _secondObserverKey = new ObserverKey("Second observer", "Some event store", "Some namespace", "Some event sequence");
        _secondObserver = Substitute.For<IObserver>();
        _secondObserver
            .When(_ => _.Handle(Arg.Any<Key>(), Arg.Any<IEnumerable<AppendedEvent>>()))
            .Do(callInfo =>
            {
                var key = callInfo.Arg<Key>();
                var events = callInfo.Arg<IEnumerable<AppendedEvent>>();
                if (!_secondObserverHandledEventsPerPartition.TryGetValue(key, out var handledEvents))
                {
                    handledEvents = [];
                    _secondObserverHandledEventsPerPartition.TryAdd(key, handledEvents);
                }
                handledEvents.Add(new(key, events));
            });
        _grainFactory.GetGrain<IObserver>(_secondObserverKey).Returns(_secondObserver);
    }
}
