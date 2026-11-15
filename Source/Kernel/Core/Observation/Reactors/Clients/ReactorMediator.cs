// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
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
    readonly ConcurrentDictionary<ReactorMediatorKey, ReactorReplayObserver> _replayObservers = new();
    readonly ConcurrentDictionary<ReactorMediatorKey, ReactorPageObserver> _pageObservers = new();
    readonly ConcurrentDictionary<ReactorMediatorKey, PageDeliveryDefinition> _pageDeliveries = new();

    /// <inheritdoc/>
    public void Subscribe(
        ReactorId reactorId,
        ConnectionId connectionId,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ReactorEventsObserver target)
    {
        var key = new ReactorMediatorKey(reactorId, connectionId, eventStore, @namespace);
        _observers[key] = target;

        // A stream that replaces an earlier one starts without that stream's page delegate; it subscribes to pages itself if it wants them.
        _pageObservers.TryRemove(key, out _);
        _pageDeliveries.TryRemove(key, out _);
    }

    /// <inheritdoc/>
    public void SubscribePages(
        ReactorId reactorId,
        ConnectionId connectionId,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ReactorPageObserver target,
        PageDeliveryDefinition? pageDelivery = null)
    {
        var key = new ReactorMediatorKey(reactorId, connectionId, eventStore, @namespace);
        _pageDeliveries[key] = pageDelivery ?? new();
        _pageObservers[key] = target;
    }

    /// <inheritdoc/>
    public PageDeliveryDefinition? GetPageDelivery(
        ReactorId reactorId,
        ConnectionId connectionId,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace)
    {
        var key = new ReactorMediatorKey(reactorId, connectionId, eventStore, @namespace);
        return _pageObservers.ContainsKey(key) && _pageDeliveries.TryGetValue(key, out var pageDelivery) ? pageDelivery : null;
    }

    /// <inheritdoc/>
    public void OnNextPage(
        ReactorId reactorId,
        ConnectionId connectionId,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        IReadOnlyList<ObserverPartitionBatch> batches,
        IReadOnlyDictionary<Key, TaskCompletionSource<ObserverSubscriberResult>> completions,
        TimeSpan? deliverWithin = null,
        ObservationOrigin origin = ObservationOrigin.Unknown)
    {
        var key = new ReactorMediatorKey(reactorId, connectionId, eventStore, @namespace);
        if (_pageObservers.TryGetValue(key, out var pageObserver))
        {
            pageObserver(batches, completions, deliverWithin, origin);
            return;
        }

        // The client did not declare pages, so it gets one partition at a time.
        foreach (var batch in batches)
        {
            OnNext(reactorId, connectionId, eventStore, @namespace, batch.Partition, batch.Events, completions[batch.Partition], deliverWithin, origin);
        }
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
        TaskCompletionSource<ObserverSubscriberResult> taskCompletionSource,
        TimeSpan? deliverWithin = null,
        ObservationOrigin origin = ObservationOrigin.Unknown)
    {
        if (_observers.TryGetValue(new(reactorId, connectionId, eventStore, @namespace), out var observable))
        {
            observable(partition, events, taskCompletionSource, deliverWithin, origin);
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
    public bool Disconnected(
        ReactorId reactorId,
        ConnectionId connectionId,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        ReactorEventsObserver target,
        ReactorReplayObserver replayTarget)
    {
        var key = new ReactorMediatorKey(reactorId, connectionId, eventStore, @namespace);
        var ownsEvents = RemoveIfOwned(_observers, key, target);
        if (ownsEvents)
        {
            _pageObservers.TryRemove(key, out _);
            _pageDeliveries.TryRemove(key, out _);
        }

        RemoveIfOwned(_replayObservers, key, replayTarget);
        return ownsEvents;
    }

    static bool RemoveIfOwned<TObserver>(ConcurrentDictionary<ReactorMediatorKey, TObserver> observers, ReactorMediatorKey key, TObserver target)
        where TObserver : Delegate
    {
        if (!observers.TryGetValue(key, out var current))
        {
            return true;
        }

        return ReferenceEquals(current, target) && (observers.TryRemove(new KeyValuePair<ReactorMediatorKey, TObserver>(key, current)) || !observers.ContainsKey(key));
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
