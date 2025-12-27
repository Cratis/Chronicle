// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using NJsonSchema;

namespace Cratis.Chronicle.Storage.EventTypes;

/// <summary>
/// Defines the store for event schemas.
/// </summary>
public interface IEventTypesStorage
{
    /// <summary>
    /// Populate the event types with existing schemas.
    /// </summary>
    /// <returns>Async task.</returns>
    Task Populate();

    /// <summary>
    /// Register a <see cref="JsonSchema"/> for a specific <see cref="EventType"/>.
    /// </summary>
    /// <param name="type"><see cref="EventType"/> to register for.</param>
    /// <param name="schema"><see cref="JsonSchema"/> to register.</param>
    /// <param name="owner">The <see cref="EventTypeOwner">owner</see> of the event type.</param>
    /// <param name="source">The <see cref="EventTypeSource">source</see> of the event type.</param>
    /// <returns>Async task.</returns>
    Task Register(EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code);

    /// <summary>
    /// Get the latest <see cref="EventTypeSchema">event schema</see> for all registered <see cref="EventType">event types</see>.
    /// </summary>
    /// <returns>A collection of <see cref="EventTypeSchema">event schemas</see>.</returns>
    Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes();

    /// <summary>
    /// Observe the latest <see cref="EventTypeSchema">event schema</see> for all registered <see cref="EventType">event types</see>.
    /// </summary>
    /// <returns>Subject with all event type schemas.</returns>
    ISubject<IEnumerable<EventTypeSchema>> ObserveLatestForAllEventTypes();

    /// <summary>
    /// Get all the <see cref="EventTypeSchema">event schemas</see> for all generations for a specific <see cref="EventType"/>.
    /// </summary>
    /// <param name="eventType"><see cref="EventType"/> to get for.</param>
    /// <returns>A collection of <see cref="EventTypeSchema">event schemas</see> - one item per generation.</returns>
    Task<IEnumerable<EventTypeSchema>> GetAllGenerationsForEventType(EventType eventType);

    /// <summary>
    /// Check if an <see cref="EventTypeSchema"/> for a specific <see cref="EventType"/> exists.
    /// </summary>
    /// <param name="type"><see cref="EventTypeId"/> to check for.</param>
    /// <param name="generation">Optional <see cref="EventTypeGeneration"/>.</param>
    /// <returns>True if there is a schema for the type, false if not.</returns>
    /// <remarks>
    /// If generation is not provided, it will get what is associated with the <see cref="EventType"/>.
    /// </remarks>
    Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default);

    /// <summary>
    /// Gets a <see cref="EventTypeSchema"/> for a specific <see cref="Type"/>.
    /// </summary>
    /// <param name="type"><see cref="EventTypeId"/> to get for.</param>
    /// <param name="generation">Optional <see cref="EventTypeGeneration"/>.</param>
    /// <returns><see cref="EventTypeSchema"/> for the type.</returns>
    /// <remarks>
    /// If generation is not provided, it will get what is associated with the <see cref="EventType"/>.
    /// </remarks>
    Task<EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default);
}
