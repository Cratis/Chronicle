// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReactorMediator"/>.
/// </summary>
[Singleton]
public class ReactorMediator : IReactorMediator
{
    readonly ConcurrentDictionary<ReactorMediatorKey, ReactorEventsObserver> _observers = new();

    /// <inheritdoc/>
    public void Subscribe(
        ReactorId reactorId,
        ConnectionId connectionId,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ReactorEventsObserver target)
    {
        _observers[new(reactorId, connectionId, eventStore, @namespace)] = target;
    }

    /// <inheritdoc/>
    public void OnNext(
        ReactorId reactorId,
        ConnectionId connectionId,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        Key partition,
        IEnumerable<AppendedEvent> events,
        TaskCompletionSource<ObserverSubscriberResult> taskCompletionSource)
    {
        if (_observers.TryGetValue(new(reactorId, connectionId, eventStore, @namespace), out var observable))
        {
            observable(partition, events, taskCompletionSource);
        }
        else
        {
            taskCompletionSource.SetResult(ObserverSubscriberResult.Disconnected());
        }
    }

    /// <inheritdoc/>
    public void Disconnected(
        ReactorId reactorId,
        ConnectionId connectionId,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace)
    {
        _observers.TryRemove(new(reactorId, connectionId, eventStore, @namespace), out var _);
    }
}
