// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Observation.Reactors.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReactorMediator"/>.
/// </summary>
[Singleton]
public class ReactorMediator : IReactorMediator
{
    readonly ConcurrentDictionary<ReactorMediatorKey, ReactorEventsObserver> _observers = new();
    readonly ConcurrentDictionary<ReactorMediatorKey, ReactorReplayObserver> _replayObservers = new();

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
    public void SubscribeReplayNotifications(
        ReactorId reactorId,
        ConnectionId connectionId,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ReactorReplayObserver target)
    {
        _replayObservers[new(reactorId, connectionId, eventStore, @namespace)] = target;
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
    public void OnBeginReplay(ReactorId reactorId, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        NotifyReplayObservers(reactorId, eventStore, @namespace, ReplayState.BeginReplay, string.Empty);

    /// <inheritdoc/>
    public void OnEndReplay(ReactorId reactorId, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        NotifyReplayObservers(reactorId, eventStore, @namespace, ReplayState.EndReplay, string.Empty);

    /// <inheritdoc/>
    public void OnBeginReplayPartition(ReactorId reactorId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Key partition) =>
        NotifyReplayObservers(reactorId, eventStore, @namespace, ReplayState.BeginReplayPartition, partition.Value.ToString()!);

    /// <inheritdoc/>
    public void OnEndReplayPartition(ReactorId reactorId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Key partition) =>
        NotifyReplayObservers(reactorId, eventStore, @namespace, ReplayState.EndReplayPartition, partition.Value.ToString()!);

    /// <inheritdoc/>
    public void Disconnected(
        ReactorId reactorId,
        ConnectionId connectionId,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace)
    {
        var key = new ReactorMediatorKey(reactorId, connectionId, eventStore, @namespace);
        _observers.TryRemove(key, out _);
        _replayObservers.TryRemove(key, out _);
    }

    void NotifyReplayObservers(ReactorId reactorId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReplayState replayState, string partition)
    {
        foreach (var (key, observer) in _replayObservers)
        {
            if (key.ReactorId == reactorId && key.EventStore == eventStore && key.Namespace == @namespace)
            {
                observer(replayState, partition);
            }
        }
    }
}
