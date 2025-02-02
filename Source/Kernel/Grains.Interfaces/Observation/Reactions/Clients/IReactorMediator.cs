// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reactors;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Clients;

/// <summary>
/// Defines a system that acts as an in-memory mediator between the actual client connected and an observer subscriber.
/// </summary>
public interface IReactorMediator
{
    /// <summary>
    /// Subscribe to events for a specific <see cref="ConnectionId"/>.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> to subscribe for.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to subscribe for.</param>
    /// <param name="target"><see cref="ReactorEventsObserver"/> delegate that will be called with events.</param>
    void Subscribe(ReactorId reactorId, ConnectionId connectionId, ReactorEventsObserver target);

    /// <summary>
    /// Notify that events should be observed.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> to send to.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to send to.</param>
    /// <param name="partition"><see cref="Key"/> for the partition.</param>
    /// <param name="events">Collection of <see cref="AppendedEvent"/> to observe.</param>
    /// <param name="taskCompletionSource"><see cref="TaskCompletionSource{T}"/> to return <see cref="ObserverSubscriberResult"/> to.</param>
    void OnNext(ReactorId reactorId, ConnectionId connectionId, Key partition, IEnumerable<AppendedEvent> events, TaskCompletionSource<ObserverSubscriberResult> taskCompletionSource);

    /// <summary>
    /// Notify that a client has connected.
    /// </summary>
    /// <param name="reactorId"><see cref="ReactorId"/> for the client that disconnected.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> for the client that disconnected.</param>
    void Disconnected(ReactorId reactorId, ConnectionId connectionId);
}
