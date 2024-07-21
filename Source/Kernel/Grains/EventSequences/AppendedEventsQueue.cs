// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Grains.EventSequences;

/// <summary>
/// Represents the state of <see cref="AppendedEventsQueues"/>.
/// </summary>
public class AppendedEventsQueue : Grain, IAppendedEventsQueue
{
    readonly Dictionary<EventTypeId, HashSet<ObserverKey>> _observersByEventType = [];
    readonly IGrainFactory _grainFactory;
    readonly Subject<AppendedEvent> _events = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AppendedEventsQueue"/> class.
    /// </summary>
    /// <param name="grainFactory"><see cref="IGrainFactory"/> for creating grains.</param>
    public AppendedEventsQueue(IGrainFactory grainFactory)
    {
        _grainFactory = grainFactory;
    }

    /// <inheritdoc/>
    public Task Enqueue(IEnumerable<AppendedEvent> appendedEvents)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task Subscribe(ObserverKey observerKey)
    {
        var observer = _grainFactory.GetGrain<IObserver>(observerKey);
        var eventTypes = await observer.GetEventTypes();
        foreach (var eventType in eventTypes)
        {
            if (!_observersByEventType.TryGetValue(eventType.Id, out var value))
            {
                value = [];
                _observersByEventType[eventType.Id] = value;
            }

            value.Add(observerKey);
        }
    }

    /// <inheritdoc/>
    public Task Unsubscribe(ObserverKey observerKey)
    {
        foreach (var observers in _observersByEventType.Values)
        {
            observers.Remove(observerKey);
        }

        return Task.CompletedTask;
    }
}
