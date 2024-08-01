// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Reactions;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Grains.Observation.Reactions.Clients;

/// <summary>
/// Represents an implementation of <see cref="IReactionMediator"/>.
/// </summary>
[Singleton]
public class ReactionMediator : IReactionMediator
{
    readonly ConcurrentDictionary<ReactionMediatorKey, ReactionEventsObserver> _observers = new();

    /// <inheritdoc/>
    public void Subscribe(
        ReactionId reactionId,
        ConnectionId connectionId,
        ReactionEventsObserver target)
    {
        _observers[new(reactionId, connectionId)] = target;
    }

    /// <inheritdoc/>
    public void OnNext(
        ReactionId reactionId,
        ConnectionId connectionId,
        IEnumerable<AppendedEvent> events,
        TaskCompletionSource<ObserverSubscriberResult> taskCompletionSource)
    {
        if (_observers.TryGetValue(new(reactionId, connectionId), out var observable))
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
        ReactionId reactionId,
        ConnectionId connectionId)
    {
        _observers.TryRemove(new(reactionId, connectionId), out var _);
    }
}
