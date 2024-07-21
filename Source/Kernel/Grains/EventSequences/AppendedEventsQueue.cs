// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;
using System.Reactive.Subjects;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents the state of <see cref="AppendedEventsQueues"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AppendedEventsQueue"/> class.
/// </remarks>
/// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
public class AppendedEventsQueue(IGrainFactory grainFactory) : Grain, IAppendedEventsQueue, IDisposable
{
    readonly Dictionary<ObserverKey, IObserver> _observersByObserverKey = [];
    readonly Dictionary<ObserverKey, IDisposable> _subscriptionsByObserverKey = [];
    readonly IGrainFactory _grainFactory = grainFactory;
    readonly Subject<IEnumerable<AppendedEvent>> _events = new();

    /// <inheritdoc/>
    public Task Enqueue(IEnumerable<AppendedEvent> appendedEvents)
    {
        _ = Task.Run(() => _events.OnNext(appendedEvents));

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Subscribe(ObserverKey observerKey, IEnumerable<EventType> eventTypes)
    {
        var observer = _grainFactory.GetGrain<IObserver>(observerKey);
        var eventTypeIds = eventTypes.Select(eventType => eventType.Id).ToArray();
        _observersByObserverKey[observerKey] = observer;

        async Task OnNext(IEnumerable<AppendedEvent> events)
        {
            var actualEvents = events.Where(@event => eventTypeIds.Contains(@event.Metadata.Type.Id)).ToList();
            var partition = actualEvents[0].Context.EventSourceId;
            await observer.Handle(partition, actualEvents);
        }

        _subscriptionsByObserverKey[observerKey] = _events.WhereEventTypesAre(eventTypeIds).Subscribe(events => OnNext(events).Wait());

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Unsubscribe(ObserverKey observerKey)
    {
        if (_observersByObserverKey.TryGetValue(observerKey, out _))
        {
            _observersByObserverKey.Remove(observerKey);
            _subscriptionsByObserverKey[observerKey].Dispose();
            _subscriptionsByObserverKey.Remove(observerKey);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose() => _events.Dispose();
}
