// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Grains.Observation.Reactions.Clients;

/// <summary>
/// Defines a system that acts as an in-memory mediator between the actual client connected and an observer subscriber.
/// </summary>
public interface IReactionMediator
{
    /// <summary>
    /// Subscribe to events for a specific <see cref="ConnectionId"/>.
    /// </summary>
    /// <param name="observerId"><see cref="ObserverId"/> to subscribe for.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to subscribe for.</param>
    /// <param name="target"><see cref="EventsObserver"/> delegate that will be called with events.</param>
    void Subscribe(ObserverId observerId, ConnectionId connectionId, EventsObserver target);

    /// <summary>
    /// Notify that events should be observed.
    /// </summary>
    /// <param name="observerId"><see cref="ObserverId"/> to send to.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to send to.</param>
    /// <param name="events">Collection of <see cref="AppendedEvent"/> to observe.</param>
    /// <param name="taskCompletionSource"><see cref="TaskCompletionSource{T}"/> to return <see cref="ObserverSubscriberResult"/> to.</param>
    void OnNext(ObserverId observerId, ConnectionId connectionId, IEnumerable<AppendedEvent> events, TaskCompletionSource<ObserverSubscriberResult> taskCompletionSource);

    /// <summary>
    /// Notify that a client has connected.
    /// </summary>
    /// <param name="observerId"><see cref="ObserverId"/> for the client that disconnected.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> for the client that disconnected.</param>
    void Disconnected(ObserverId observerId, ConnectionId connectionId);
}
