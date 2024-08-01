// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation.Reactions;

namespace Cratis.Chronicle.Grains.Observation.Reactions.Clients;

/// <summary>
/// Defines a system that acts as an in-memory mediator between the actual client connected and an observer subscriber.
/// </summary>
public interface IReactionMediator
{
    /// <summary>
    /// Subscribe to events for a specific <see cref="ConnectionId"/>.
    /// </summary>
    /// <param name="reactionId"><see cref="ReactionId"/> to subscribe for.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to subscribe for.</param>
    /// <param name="target"><see cref="ReactionEventsObserver"/> delegate that will be called with events.</param>
    void Subscribe(ReactionId reactionId, ConnectionId connectionId, ReactionEventsObserver target);

    /// <summary>
    /// Notify that events should be observed.
    /// </summary>
    /// <param name="reactionId"><see cref="ReactionId"/> to send to.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to send to.</param>
    /// <param name="events">Collection of <see cref="AppendedEvent"/> to observe.</param>
    /// <param name="taskCompletionSource"><see cref="TaskCompletionSource{T}"/> to return <see cref="ObserverSubscriberResult"/> to.</param>
    void OnNext(ReactionId reactionId, ConnectionId connectionId, IEnumerable<AppendedEvent> events, TaskCompletionSource<ObserverSubscriberResult> taskCompletionSource);

    /// <summary>
    /// Notify that a client has connected.
    /// </summary>
    /// <param name="reactionId"><see cref="ReactionId"/> for the client that disconnected.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> for the client that disconnected.</param>
    void Disconnected(ReactionId reactionId, ConnectionId connectionId);
}
