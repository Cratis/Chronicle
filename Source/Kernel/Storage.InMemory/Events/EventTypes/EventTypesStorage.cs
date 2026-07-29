// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Storage.InMemory.Events.EventTypes;

/// <summary>
/// Represents a no-op in-memory implementation of <see cref="IEventTypesStorage"/>.
/// </summary>
/// <remarks>
/// Returns an empty <see cref="JsonSchema"/> for every requested event type, which causes the
/// <c>ExpandoObjectConverter</c> to fall back to generic unknown-type
/// conversion - preserving all event content without schema-driven type coercion.
/// No compliance rules, migrations, or validations are applied.
/// What has been registered is remembered for the lifetime of the process, so that registering the same event
/// types again - which every client does when it reconnects - is recognized as the no-op it is.
/// </remarks>
public class EventTypesStorage : IEventTypesStorage
{
    readonly ConcurrentDictionary<EventTypeId, EventTypeDefinition> _definitions = new();

    /// <inheritdoc/>
    public Task<bool> Register(EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code) =>
        Register(new EventTypeDefinition(type.Id, owner, type.Tombstone, [new EventTypeGenerationDefinition(type.Generation, schema)], []));

    /// <inheritdoc/>
    public Task<bool> Register(EventTypeDefinition definition)
    {
        _definitions.AddOrUpdate(
            definition.Id,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => Merge(existing, incoming),
            definition);

        // There is no cache anywhere to evict for an in-memory single-node store, so a registration never asks
        // anyone to invalidate.
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes() =>
        Task.FromResult(Enumerable.Empty<EventTypeSchema>());

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventTypeSchema>> ObserveLatestForAllEventTypes() =>
        new ReplaySubject<IEnumerable<EventTypeSchema>>(1);

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeDefinition>> GetAllDefinitions() =>
        Task.FromResult<IEnumerable<EventTypeDefinition>>([.. _definitions.Values]);

    /// <inheritdoc/>
    public Task<EventTypeDefinition> GetDefinition(EventTypeId eventTypeId) =>
        Task.FromResult(new EventTypeDefinition(
            eventTypeId,
            EventTypeOwner.Client,
            false,
            [new EventTypeGenerationDefinition(EventTypeGeneration.First, new JsonSchema())],
            []));

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeSchema>> GetAllGenerationsForEventType(EventType eventType) =>
        Task.FromResult(Enumerable.Empty<EventTypeSchema>());

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventTypeId> eventTypeIds) =>
        Task.FromResult(Enumerable.Empty<EventTypeSchema>());

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventType> eventTypes) =>
        Task.FromResult(Enumerable.Empty<EventTypeSchema>());

    /// <inheritdoc/>
    public Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default) =>
        Task.FromResult(true);

    /// <inheritdoc/>
    public Task<EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        var eventType = new EventType(type, generation ?? EventTypeGeneration.First);
        return Task.FromResult(new EventTypeSchema(eventType, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema()));
    }

    /// <inheritdoc/>
    public void Invalidate(EventTypeId eventTypeId)
    {
    }

    static EventTypeDefinition Merge(EventTypeDefinition existing, EventTypeDefinition incoming) =>
        incoming with
        {
            Generations = existing.Generations
                .Where(_ => incoming.Generations.All(incomingGeneration => incomingGeneration.Generation != _.Generation))
                .Concat(incoming.Generations)
                .ToList(),
            Migrations = incoming.Migrations.Any() ? incoming.Migrations : existing.Migrations
        };
}
