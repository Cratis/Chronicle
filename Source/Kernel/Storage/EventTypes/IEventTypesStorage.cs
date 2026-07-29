// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.EventTypes;

/// <summary>
/// Defines the store for event schemas.
/// </summary>
public interface IEventTypesStorage
{
    /// <summary>
    /// Register a <see cref="JsonSchema"/> for a specific <see cref="EventType"/>.
    /// </summary>
    /// <param name="type"><see cref="EventType"/> to register for.</param>
    /// <param name="schema"><see cref="JsonSchema"/> to register.</param>
    /// <param name="owner">The <see cref="EventTypeOwner">owner</see> of the event type.</param>
    /// <param name="source">The <see cref="EventTypeSource">source</see> of the event type.</param>
    /// <returns>True if the stored event type was created or changed, false if the registration was a no-op.</returns>
    /// <remarks>
    /// The return value tells the caller whether peers must invalidate their caches: a genuine change - a new
    /// generation, or a different owner, source, or tombstone - returns true; re-registering an already-stored
    /// generation with identical metadata returns false, so routine reconnect re-registrations do not fan out.
    /// </remarks>
    Task<bool> Register(EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code);

    /// <summary>
    /// Register a complete <see cref="EventTypeDefinition"/> with all generations and migrations.
    /// </summary>
    /// <param name="definition">The <see cref="EventTypeDefinition"/> to register.</param>
    /// <returns>True if the stored event type was created or changed, false if the registration was a no-op.</returns>
    Task<bool> Register(EventTypeDefinition definition);

    /// <summary>
    /// Register a whole batch of <see cref="EventTypeToRegister">event types</see> in one operation.
    /// </summary>
    /// <param name="eventTypes">The <see cref="EventTypeToRegister">event types</see> to register.</param>
    /// <returns>The <see cref="EventTypeId">identifiers</see> of the event types whose stored representation was created or changed.</returns>
    /// <remarks>
    /// A client registers every event type it knows about at startup, so doing it one at a time costs a round trip
    /// per event type against the underlying store. Taking the whole batch lets an implementation collapse that into
    /// a single read and a single write. The default implementation registers one at a time, so an implementation
    /// only needs to override this when it can do better.
    /// Only the identifiers of event types that actually changed are returned - re-registering identical event types
    /// yields none, so routine client reconnects do not trigger cluster-wide cache eviction.
    /// </remarks>
    async Task<IEnumerable<EventTypeId>> Register(IEnumerable<EventTypeToRegister> eventTypes)
    {
        var mutated = new List<EventTypeId>();

        foreach (var eventType in eventTypes)
        {
            var definition = eventType.Definition;
            var generations = definition.Generations.ToList();

            // A single generation without migrations is exactly what the simple registration expresses - anything
            // else needs the full definition.
            var changed = generations.Count == 1 && !definition.Migrations.Any()
                ? await Register(
                    new EventType(definition.Id, generations[0].Generation, definition.Tombstone),
                    generations[0].Schema,
                    definition.Owner,
                    eventType.Source)
                : await Register(definition);

            if (changed)
            {
                mutated.Add(definition.Id);
            }
        }

        return mutated;
    }

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
    /// Get all the <see cref="EventTypeDefinition">event type definitions</see> for all registered event types.
    /// </summary>
    /// <returns>A collection of <see cref="EventTypeDefinition">event type definitions</see>.</returns>
    Task<IEnumerable<EventTypeDefinition>> GetAllDefinitions();

    /// <summary>
    /// Get the complete <see cref="EventTypeDefinition"/> for a specific event type.
    /// </summary>
    /// <param name="eventTypeId">The <see cref="EventTypeId"/> to get for.</param>
    /// <returns>The <see cref="EventTypeDefinition"/>.</returns>
    Task<EventTypeDefinition> GetDefinition(EventTypeId eventTypeId);

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

    /// <summary>
    /// Gets a collection of <see cref="EventTypeSchema"/> for a collection of <see cref="EventTypeId"/>.
    /// </summary>
    /// <param name="eventTypeIds">The <see cref="EventTypeId"/> collection to get for.</param>
    /// <returns>A collection of <see cref="EventTypeSchema"/>, one per matched type.</returns>
    Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventTypeId> eventTypeIds);

    /// <summary>
    /// Gets a collection of <see cref="EventTypeSchema"/> for a collection of <see cref="EventType"/>.
    /// </summary>
    /// <param name="eventTypes">The <see cref="EventType"/> collection to get for.</param>
    /// <returns>A collection of <see cref="EventTypeSchema"/>, one per matched type respecting each type's generation.</returns>
    Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventType> eventTypes);

    /// <summary>
    /// Evict any cached schema and definition information held for a specific <see cref="EventTypeId"/>.
    /// </summary>
    /// <param name="eventTypeId">The <see cref="EventTypeId"/> to evict.</param>
    /// <remarks>
    /// Called locally after a registration and on every silo when an event type is registered elsewhere in the
    /// cluster, so a peer cannot keep serving a stale definition after a new generation is added. Implementations
    /// without an in-memory cache treat this as a no-op.
    /// </remarks>
    void Invalidate(EventTypeId eventTypeId);
}
