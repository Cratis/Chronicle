// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Schema;

namespace Cratis.Events.Schemas
{
    /// <summary>
    /// Defines the store for event schemas.
    /// </summary>
    public interface ISchemaStore
    {
        /// <summary>
        /// Register a <see cref="JSchema"/> for a specific <see cref="EventType"/>.
        /// </summary>
        /// <param name="type"><see cref="EventType"/> to register for.</param>
        /// <param name="friendlyName">A friendly name to identify the event with.</param>
        /// <param name="schema"><see cref="JSchema"/> to register.</param>
        /// <returns>Async task.</returns>
        Task Register(EventType type, string friendlyName, JSchema schema);

        /// <summary>
        /// Get the latest <see cref="EventSchema">event schema</see> for all registered <see cref="EventType">event types</see>.
        /// </summary>
        /// <returns>A collection of <see cref="EventSchema">event schemas</see>.</returns>
        Task<IEnumerable<EventSchema>> GetLatestForAllEventTypes();

        /// <summary>
        /// Get all the <see cref="EventSchema">event schemas</see> for all generations for a specific <see cref="EventType"/>.
        /// </summary>
        /// <param name="eventType"><see cref="EventType"/> to get for.</param>
        /// <returns>A collection of <see cref="EventSchema">event schemas</see> - one item per generation.</returns>
        Task<IEnumerable<EventSchema>> GetAllGenerationsForEventType(EventType eventType);

        /// <summary>
        /// Check if an <see cref="EventSchema"/> for a specific <see cref="EventType"/> exists.
        /// </summary>
        /// <param name="type"><see cref="EventTypeId"/> to check for.</param>
        /// <param name="generation">Optional <see cref="EventGeneration"/>.</param>
        /// <returns>True if there is a schema for the type, false if not.</returns>
        /// <remarks>
        /// If generation is not provided, it will get what is associated with the <see cref="EventType"/>.
        /// </remarks>
        Task<bool> HasFor(EventTypeId type, EventGeneration? generation = default);

        /// <summary>
        /// Gets a <see cref="EventSchema"/> for a specific <see cref="Type"/>.
        /// </summary>
        /// <param name="type"><see cref="EventTypeId"/> to get for.</param>
        /// <param name="generation">Optional <see cref="EventGeneration"/>.</param>
        /// <returns><see cref="EventSchema"/> for the type.</returns>
        /// <remarks>
        /// If generation is not provided, it will get what is associated with the <see cref="EventType"/>.
        /// </remarks>
        Task<EventSchema> GetFor(EventTypeId type, EventGeneration? generation = default);
    }
}
