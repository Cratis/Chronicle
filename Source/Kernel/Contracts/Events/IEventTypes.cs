// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Defines the service contract for working with event types.
/// </summary>
[Service]
public interface IEventTypes
{
    /// <summary>
    /// Register a collection of event types.
    /// </summary>
    /// <param name="request">The <see cref="RegisterEventTypesRequest"/> payload.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task Register(RegisterEventTypesRequest request);

    /// <summary>
    /// Register a single event type.
    /// </summary>
    /// <param name="request">The <see cref="RegisterSingleEventTypeRequest"/> payload.</param>
    /// <returns>Awaitable task.</returns>
    [Operation]
    Task RegisterSingle(RegisterSingleEventTypeRequest request);

    /// <summary>
    /// Get all the registered event types for an event store.
    /// </summary>
    /// <param name="request">The <see cref="GetAllEventTypesRequest"/> payload.</param>
    /// <returns>A collection of <see cref="EventType"/> instances.</returns>
    [Operation]
    Task<IEnumerable<EventType>> GetAll(GetAllEventTypesRequest request);

    /// <summary>
    /// Get all the registered event types for an event store with full registration information.
    /// </summary>
    /// <param name="request">The <see cref="GetAllEventTypesRequest"/> payload.</param>
    /// <returns>A collection of <see cref="EventTypeRegistration"/> instances.</returns>
    [Operation]
    Task<IEnumerable<EventTypeRegistration>> GetAllRegistrations(GetAllEventTypesRequest request);

    /// <summary>
    /// Observe all the registered event types for an event store with full registration information.
    /// </summary>
    /// <param name="request">The <see cref="GetAllEventTypesRequest"/> payload.</param>
    /// <param name="context">The gRPC <see cref="CallContext"/>.</param>
    /// <returns>An observable of collection of <see cref="EventTypeRegistration"/> instances.</returns>
    [Operation]
    IObservable<IEnumerable<EventTypeRegistration>> ObserveAllRegistrations(GetAllEventTypesRequest request, CallContext context = default);
}
