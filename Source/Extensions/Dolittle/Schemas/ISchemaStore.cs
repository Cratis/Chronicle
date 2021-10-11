// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;
using Dolittle.SDK.Artifacts;

namespace Cratis.Extensions.Dolittle.Schemas
{
    /// <summary>
    /// Defines the store for event schemas.
    /// </summary>
    public interface ISchemaStore
    {
        /// <summary>
        /// Discover all event types, generate schemas for them and consolidate with what is in the store already.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task DiscoverGenerateAndConsolidate();

        /// <summary>
        /// Generate a schema for a specific type.
        /// </summary>
        /// <param name="type"><see cref="Type"/> to define from.</param>
        /// <returns><see cref="EventSchema"/> for the type.</returns>
        EventSchema GenerateFor(Type type);

        /// <summary>
        /// Get the latest <see cref="EventSchema">event schema</see> for all registered <see cref="EventType">event types</see>.
        /// </summary>
        /// <returns>A collection of <see cref="EventSchema">event schemas</see>.</returns>
        Task<IEnumerable<EventSchema>> GetLatestForAllEventTypes();

        /// <summary>
        /// Get all the <see cref="EventSchema">event schemas</see> for all generations for a specific <see cref="global::Dolittle.SDK.Events.EventType"/>.
        /// </summary>
        /// <param name="eventType"><see cref="global::Dolittle.SDK.Events.EventType"/> to get for.</param>
        /// <returns>A collection of <see cref="EventSchema">event schemas</see> - one item per generation.</returns>
        Task<IEnumerable<EventSchema>> GetAllGenerationsForEventType(global::Dolittle.SDK.Events.EventType eventType);

        /// <summary>
        /// Save an <see cref="EventSchema"/> to the store.
        /// </summary>
        /// <param name="eventSchema"><see cref="EventSchema"/> to save.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task Save(EventSchema eventSchema);

        /// <summary>
        /// Check if an <see cref="EventSchema"/> for a specific <see cref="EventType"/> exists.
        /// </summary>
        /// <param name="type"><see cref="global::Dolittle.SDK.Events.EventType"/> to check for.</param>
        /// <param name="generation">Optional <see cref="Generation"/>.</param>
        /// <returns>True if there is a schema for the type, false if not.</returns>
        /// <remarks>
        /// If generation is not provided, it will get what is associated with the <see cref="EventType"/>.
        /// </remarks>
        Task<bool> HasFor(global::Dolittle.SDK.Events.EventType type, Generation? generation = default);

        /// <summary>
        /// Gets a <see cref="EventSchema"/> for a specific <see cref="Type"/>.
        /// </summary>
        /// <param name="type"><see cref="global::Dolittle.SDK.Events.EventType"/> to get for.</param>
        /// <param name="generation">Optional <see cref="Generation"/>.</param>
        /// <returns><see cref="EventSchema"/> for the type.</returns>
        /// <remarks>
        /// If generation is not provided, it will get what is associated with the <see cref="EventType"/>.
        /// </remarks>
        Task<EventSchema> GetFor(global::Dolittle.SDK.Events.EventType type, Generation? generation = default);
    }
}
