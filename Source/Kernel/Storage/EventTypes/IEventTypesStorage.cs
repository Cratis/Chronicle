// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;
using Aksio.Cratis.EventTypes;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Storage.EventTypes;

/// <summary>
/// Defines the store for event schemas.
/// </summary>
public interface IEventTypesStorage
{
    /// <summary>
    /// Populate the schema store with existing schemas.
    /// </summary>
    /// <returns>Async task.</returns>
    Task Populate();

    /// <summary>
    /// Register a <see cref="JsonSchema"/> for a specific <see cref="EventType"/>.
    /// </summary>
    /// <param name="type"><see cref="EventType"/> to register for.</param>
    /// <param name="friendlyName">A friendly name to identify the event with.</param>
    /// <param name="schema"><see cref="JsonSchema"/> to register.</param>
    /// <returns>Async task.</returns>
    Task Register(EventType type, string friendlyName, JsonSchema schema);

    /// <summary>
    /// Get the latest <see cref="EventTypeSchema">event schema</see> for all registered <see cref="EventType">event types</see>.
    /// </summary>
    /// <returns>A collection of <see cref="EventTypeSchema">event schemas</see>.</returns>
    Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes();

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
    /// <param name="generation">Optional <see cref="EventGeneration"/>.</param>
    /// <returns>True if there is a schema for the type, false if not.</returns>
    /// <remarks>
    /// If generation is not provided, it will get what is associated with the <see cref="EventType"/>.
    /// </remarks>
    Task<bool> HasFor(EventTypeId type, EventGeneration? generation = default);

    /// <summary>
    /// Gets a <see cref="EventTypeSchema"/> for a specific <see cref="Type"/>.
    /// </summary>
    /// <param name="type"><see cref="EventTypeId"/> to get for.</param>
    /// <param name="generation">Optional <see cref="EventGeneration"/>.</param>
    /// <returns><see cref="EventTypeSchema"/> for the type.</returns>
    /// <remarks>
    /// If generation is not provided, it will get what is associated with the <see cref="EventType"/>.
    /// </remarks>
    Task<EventTypeSchema> GetFor(EventTypeId type, EventGeneration? generation = default);
}
