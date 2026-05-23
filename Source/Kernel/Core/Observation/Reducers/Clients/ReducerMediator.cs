// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Dynamic;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReducerMediator"/>.
/// </summary>
[Singleton]
public class ReducerMediator : IReducerMediator
{
    readonly ConcurrentDictionary<ReducerMediatorKey, ReducerEventsObserver> _observers = new();
    readonly ConcurrentDictionary<ReducerMediatorKey, ReducerReplayObserver> _replayObservers = new();

    /// <inheritdoc/>
    public void Subscribe(
        ReducerId reducerId,
        ConnectionId connectionId,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ReducerEventsObserver target)
    {
        _observers[new(reducerId, connectionId, eventStore, @namespace)] = target;
    }

    /// <inheritdoc/>
    public void SubscribeReplayNotifications(
        ReducerId reducerId,
        ConnectionId connectionId,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ReducerReplayObserver target)
    {
        _replayObservers[new(reducerId, connectionId, eventStore, @namespace)] = target;
    }

    /// <inheritdoc/>
    public void OnNext(
        ReducerId reducerId,
        ConnectionId connectionId,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ReduceOperation operation,
        TaskCompletionSource<ReducerSubscriberResult> taskCompletionSource)
    {
        if (_observers.TryGetValue(new(reducerId, connectionId, eventStore, @namespace), out var observable))
        {
            observable(operation, taskCompletionSource);
        }
        else
        {
            taskCompletionSource.SetResult(new(ObserverSubscriberResult.Disconnected(), new ExpandoObject()));
        }
    }

    /// <inheritdoc/>
    public void OnBeginReplay(ReducerId reducerId, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        NotifyReplayObservers(reducerId, eventStore, @namespace, ReplayState.BeginReplay, string.Empty);

    /// <inheritdoc/>
    public void OnEndReplay(ReducerId reducerId, EventStoreName eventStore, EventStoreNamespaceName @namespace) =>
        NotifyReplayObservers(reducerId, eventStore, @namespace, ReplayState.EndReplay, string.Empty);

    /// <inheritdoc/>
    public void OnBeginReplayPartition(ReducerId reducerId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Key partition) =>
        NotifyReplayObservers(reducerId, eventStore, @namespace, ReplayState.BeginReplayPartition, partition.Value.ToString()!);

    /// <inheritdoc/>
    public void OnEndReplayPartition(ReducerId reducerId, EventStoreName eventStore, EventStoreNamespaceName @namespace, Key partition) =>
        NotifyReplayObservers(reducerId, eventStore, @namespace, ReplayState.EndReplayPartition, partition.Value.ToString()!);

    /// <inheritdoc/>
    public void Disconnected(
        ReducerId reducerId,
        ConnectionId connectionId,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace)
    {
        var key = new ReducerMediatorKey(reducerId, connectionId, eventStore, @namespace);
        _observers.TryRemove(key, out _);
        _replayObservers.TryRemove(key, out _);
    }

    void NotifyReplayObservers(ReducerId reducerId, EventStoreName eventStore, EventStoreNamespaceName @namespace, ReplayState replayState, string partition)
    {
        foreach (var (key, observer) in _replayObservers)
        {
            if (key.ReducerId == reducerId && key.EventStore == eventStore && key.Namespace == @namespace)
            {
                observer(replayState, partition);
            }
        }
    }
}
