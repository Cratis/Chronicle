// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// 16.45.x compatibility surface - do not remove.
//
// This service interface was the client contract up to and including 16.45.x and was replaced by
// the generated 17+ surface. Compiled consumers (e.g. Cratis.Stage 3.11.0's generated type
// bindings) hold assembly references to it and fail to load when it is absent. The [Service] and
// [Operation] attributes are deliberately stripped: the 18.x kernel does not serve this service,
// and the canonical descriptor set must not advertise it - the types exist for assembly-level
// compatibility only, not for the wire.
namespace Cratis.Chronicle.Contracts.Events;

/// <summary>
/// Defines the service contract for working with event types.
/// </summary>
public interface IEventTypes
{
    /// <summary>
    /// Register a collection of event types.
    /// </summary>
    /// <param name="request">The <see cref="RegisterEventTypesRequest"/> payload.</param>
    /// <returns>Awaitable task.</returns>
    Task Register(RegisterEventTypesRequest request);

    /// <summary>
    /// Register a single event type.
    /// </summary>
    /// <param name="request">The <see cref="RegisterSingleEventTypeRequest"/> payload.</param>
    /// <returns>Awaitable task.</returns>
    Task RegisterSingle(RegisterSingleEventTypeRequest request);

    /// <summary>
    /// Get all the registered event types for an event store.
    /// </summary>
    /// <param name="request">The <see cref="GetAllEventTypesRequest"/> payload.</param>
    /// <returns>A collection of <see cref="EventType"/> instances.</returns>
    Task<IEnumerable<EventType>> GetAll(GetAllEventTypesRequest request);

    /// <summary>
    /// Get all the registered event types for an event store with full registration information.
    /// </summary>
    /// <param name="request">The <see cref="GetAllEventTypesRequest"/> payload.</param>
    /// <returns>A collection of <see cref="EventTypeRegistration"/> instances.</returns>
    Task<IEnumerable<EventTypeRegistration>> GetAllRegistrations(GetAllEventTypesRequest request);

    /// <summary>
    /// Observe all the registered event types for an event store with full registration information.
    /// </summary>
    /// <param name="request">The <see cref="GetAllEventTypesRequest"/> payload.</param>
    /// <param name="context">The gRPC <see cref="CallContext"/>.</param>
    /// <returns>An observable of collection of <see cref="EventTypeRegistration"/> instances.</returns>
    IObservable<IEnumerable<EventTypeRegistration>> ObserveAllRegistrations(GetAllEventTypesRequest request, CallContext context = default);

    /// <summary>
    /// Get all the registered generations for a specific event type.
    /// </summary>
    /// <param name="request">The <see cref="GetEventTypeGenerationsRequest"/> payload.</param>
    /// <returns>A collection of <see cref="EventTypeRegistration"/> instances, one per generation.</returns>
    Task<IEnumerable<EventTypeRegistration>> GetAllGenerationsForEventType(GetEventTypeGenerationsRequest request);
}
