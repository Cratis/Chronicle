// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Grains.Observation.Reactions.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReactionMediator"/>.
/// </summary>
[Singleton]
public class ReactionMediator : IReactionMediator
{
    readonly ConcurrentDictionary<ReactionMediatorKey, EventsObserver> _observers = new();

    /// <inheritdoc/>
    public void Subscribe(
        ObserverId observerId,
        ConnectionId connectionId,
        EventsObserver target)
    {
        _observers[new(observerId, connectionId)] = target;
    }

    /// <inheritdoc/>
    public void OnNext(
        ObserverId observerId,
        ConnectionId connectionId,
        IEnumerable<AppendedEvent> events,
        TaskCompletionSource<ObserverSubscriberResult> taskCompletionSource)
    {
        if (_observers.TryGetValue(new(observerId, connectionId), out var observable))
        {
            observable(events, taskCompletionSource);
        }
        else
        {
            taskCompletionSource.SetResult(ObserverSubscriberResult.Disconnected(EventSequenceNumber.Unavailable));
        }
    }

    /// <inheritdoc/>
    public void Disconnected(
        ObserverId observerId,
        ConnectionId connectionId)
    {
        _observers.TryRemove(new(observerId, connectionId), out var _);
    }
}
