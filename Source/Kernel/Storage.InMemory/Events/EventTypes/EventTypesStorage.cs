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
/// Represents an in-memory implementation of <see cref="IEventTypesStorage"/>.
/// </summary>
/// <remarks>
/// <para>
/// What has been registered is remembered for the lifetime of the process and read back, so tooling that lists event
/// types - the Workbench among them - sees the same registrations against in-memory storage as against a database.
/// No compliance rules, migrations, or validations are applied.
/// </para>
/// <para>
/// An event type that was never registered still resolves to an empty <see cref="JsonSchema"/> rather than failing,
/// which causes the <c>ExpandoObjectConverter</c> to fall back to generic unknown-type conversion - preserving all
/// event content without schema-driven type coercion.
/// </para>
/// </remarks>
public class EventTypesStorage : IEventTypesStorage, IDisposable
{
    readonly ConcurrentDictionary<EventTypeId, EventTypeDefinition> _definitions = new();
    readonly ConcurrentDictionary<EventTypeId, EventTypeSource> _sources = new();
    readonly Subject<IEnumerable<EventTypeSchema>> _changes = new();

    /// <inheritdoc/>
    public Task<bool> Register(EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code)
    {
        _sources[type.Id] = source;

        return Register(new EventTypeDefinition(type.Id, owner, type.Tombstone, [new EventTypeGenerationDefinition(type.Generation, schema)], []));
    }

    /// <inheritdoc/>
    public Task<bool> Register(EventTypeDefinition definition)
    {
        _definitions.AddOrUpdate(
            definition.Id,
            static (_, incoming) => incoming,
            static (_, existing, incoming) => Merge(existing, incoming),
            definition);

        _changes.OnNext(Latest());

        // There is no cache anywhere to evict for an in-memory single-node store, so a registration never asks
        // anyone to invalidate.
        return Task.FromResult(false);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes() => Task.FromResult(Latest());

    /// <inheritdoc/>
    /// <remarks>
    /// Each call gets its own subject seeded with the current registrations. Callers complete the subject they are
    /// given when their connection goes away, so a shared one would end observation for everyone in the process.
    /// </remarks>
    public ISubject<IEnumerable<EventTypeSchema>> ObserveLatestForAllEventTypes()
    {
        var subject = new ReplaySubject<IEnumerable<EventTypeSchema>>(1);
        subject.OnNext(Latest());

        var subscription = _changes.Subscribe(subject.OnNext);
        subject.Subscribe(_ => { }, _ => { }, subscription.Dispose);

        return subject;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeDefinition>> GetAllDefinitions() =>
        Task.FromResult<IEnumerable<EventTypeDefinition>>([.. _definitions.Values]);

    /// <inheritdoc/>
    public Task<EventTypeDefinition> GetDefinition(EventTypeId eventTypeId) =>
        Task.FromResult(_definitions.TryGetValue(eventTypeId, out var definition)
            ? definition
            : new EventTypeDefinition(
                eventTypeId,
                EventTypeOwner.Client,
                false,
                [new EventTypeGenerationDefinition(EventTypeGeneration.First, new JsonSchema())],
                []));

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeSchema>> GetAllGenerationsForEventType(EventType eventType) =>
        Task.FromResult(_definitions.TryGetValue(eventType.Id, out var definition)
            ? definition.Generations.Select(generation => ToSchema(definition, generation))
            : []);

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventTypeId> eventTypeIds) =>
        Task.FromResult<IEnumerable<EventTypeSchema>>([.. eventTypeIds.Select(LatestFor).OfType<EventTypeSchema>()]);

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventType> eventTypes) =>
        Task.FromResult<IEnumerable<EventTypeSchema>>([.. eventTypes.Select(eventType => LatestFor(eventType.Id)).OfType<EventTypeSchema>()]);

    /// <inheritdoc/>
    public Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default) =>
        Task.FromResult(true);

    /// <inheritdoc/>
    public Task<EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        if (_definitions.TryGetValue(type, out var definition) &&
            Generation(definition, generation) is { } registered)
        {
            return Task.FromResult(ToSchema(definition, registered));
        }

        // An unregistered event type is not an error here - an empty schema makes the converter preserve the
        // content as-is rather than coerce it.
        var eventType = new EventType(type, generation ?? EventTypeGeneration.First);
        return Task.FromResult(new EventTypeSchema(eventType, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema()));
    }

    /// <inheritdoc/>
    public void Invalidate(EventTypeId eventTypeId)
    {
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _changes.Dispose();
        GC.SuppressFinalize(this);
    }

    static EventTypeGenerationDefinition? Generation(EventTypeDefinition definition, EventTypeGeneration? generation) =>
        generation is null
            ? definition.Generations.OrderByDescending(_ => _.Generation.Value).FirstOrDefault()
            : definition.Generations.FirstOrDefault(_ => _.Generation == generation);

    static EventTypeDefinition Merge(EventTypeDefinition existing, EventTypeDefinition incoming) =>
        incoming with
        {
            Generations = existing.Generations
                .Where(_ => incoming.Generations.All(incomingGeneration => incomingGeneration.Generation != _.Generation))
                .Concat(incoming.Generations)
                .ToList(),
            Migrations = incoming.Migrations.Any() ? incoming.Migrations : existing.Migrations
        };

    IEnumerable<EventTypeSchema> Latest() => [.. _definitions.Values.Select(LatestFor).OfType<EventTypeSchema>()];

    EventTypeSchema? LatestFor(EventTypeId eventTypeId) =>
        _definitions.TryGetValue(eventTypeId, out var definition) ? LatestFor(definition) : null;

    EventTypeSchema? LatestFor(EventTypeDefinition definition) =>
        Generation(definition, null) is { } generation ? ToSchema(definition, generation) : null;

    EventTypeSchema ToSchema(EventTypeDefinition definition, EventTypeGenerationDefinition generation) =>
        new(
            new EventType(definition.Id, generation.Generation, definition.Tombstone),
            definition.Owner,
            _sources.TryGetValue(definition.Id, out var source) ? source : EventTypeSource.Code,
            generation.Schema);
}
