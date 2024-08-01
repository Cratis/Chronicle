// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;
using Cratis.Chronicle.Concepts.Observation.Reducers;

namespace Cratis.Chronicle.Grains.Observation.Reducers.Clients;

/// <summary>
/// Defines a system that acts as an in-memory mediator between the actual client connected and an observer subscriber.
/// </summary>
public interface IReducerMediator
{
    /// <summary>
    /// Subscribe to events for a specific <see cref="ConnectionId"/>.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> to subscribe for.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to subscribe for.</param>
    /// <param name="target"><see cref="ReducerEventsObserver"/> delegate that will be called with events.</param>
    void Subscribe(ReducerId reducerId, ConnectionId connectionId, ReducerEventsObserver target);

    /// <summary>
    /// Notify that events should be observed.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> to send to.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> to send to.</param>
    /// <param name="operation"><see cref="ReduceOperation"/> to send.</param>
    /// <param name="taskCompletionSource"><see cref="TaskCompletionSource{T}"/> to return <see cref="ObserverSubscriberResult"/> to.</param>
    void OnNext(ReducerId reducerId, ConnectionId connectionId, ReduceOperation operation, TaskCompletionSource<ReducerSubscriberResult> taskCompletionSource);

    /// <summary>
    /// Notify that a client has connected.
    /// </summary>
    /// <param name="reducerId"><see cref="ReducerId"/> for the client that disconnected.</param>
    /// <param name="connectionId"><see cref="ConnectionId"/> for the client that disconnected.</param>
    void Disconnected(ReducerId reducerId, ConnectionId connectionId);
}
