// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;

namespace Aksio.Cratis.Kernel.Grains.Observation.Clients;

/// <summary>
/// Represents an implementation of <see cref="IObserverMediator"/>.
/// </summary>
[Singleton]
public class ObserverMediator : IObserverMediator
{
    readonly ConcurrentDictionary<ConnectionId, EventsObserver> _observables = new();

    /// <inheritdoc/>
    public void Subscribe(ConnectionId connectionId, EventsObserver target)
    {
        _observables[connectionId] = target;
    }

    /// <inheritdoc/>
    public void OnNext(ConnectionId connectionId, IEnumerable<AppendedEvent> events, TaskCompletionSource<ObserverSubscriberResult> taskCompletionSource)
    {
        if (_observables.TryGetValue(connectionId, out var observable))
        {
            observable(events, taskCompletionSource);
        }
        else
        {
            taskCompletionSource.SetResult(ObserverSubscriberResult.Disconnected(EventSequenceNumber.Unavailable));
        }
    }

    /// <inheritdoc/>
    public void Disconnected(ConnectionId connectionId)
    {
        _observables.TryRemove(connectionId, out var _);
    }
}
