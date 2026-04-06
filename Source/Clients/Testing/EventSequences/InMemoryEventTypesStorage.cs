// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;

using System.Reactive.Subjects;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;
using KernelConcepts::Cratis.Chronicle.Concepts.Events;
using KernelEventTypes = KernelConcepts::Cratis.Chronicle.Concepts.EventTypes;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Represents a no-op in-memory implementation of <see cref="IEventTypesStorage"/> for testing.
/// </summary>
/// <remarks>
/// Returns an empty <see cref="JsonSchema"/> for every requested event type, which causes the
/// <see cref="Cratis.Chronicle.Json.ExpandoObjectConverter"/> to fall back to generic unknown-type
/// conversion — preserving all event content without schema-driven type coercion.
/// No compliance rules, migrations, or validations are applied.
/// </remarks>
internal sealed class InMemoryEventTypesStorage : IEventTypesStorage
{
    /// <inheritdoc/>
    public Task Register(EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code) =>
        Task.CompletedTask;

    /// <inheritdoc/>
    public Task Register(EventTypeDefinition definition) => Task.CompletedTask;

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetLatestForAllEventTypes() =>
        Task.FromResult(Enumerable.Empty<KernelEventTypes::EventTypeSchema>());

    /// <inheritdoc/>
    public ISubject<IEnumerable<KernelEventTypes::EventTypeSchema>> ObserveLatestForAllEventTypes() =>
        new Subject<IEnumerable<KernelEventTypes::EventTypeSchema>>();

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeDefinition>> GetAllDefinitions() =>
        Task.FromResult(Enumerable.Empty<EventTypeDefinition>());

    /// <inheritdoc/>
    public Task<EventTypeDefinition> GetDefinition(EventTypeId eventTypeId) =>
        Task.FromResult(new EventTypeDefinition(eventTypeId, EventTypeOwner.Client, false, [], []));

    /// <inheritdoc/>
    public Task<IEnumerable<KernelEventTypes::EventTypeSchema>> GetAllGenerationsForEventType(EventType eventType) =>
        Task.FromResult(Enumerable.Empty<KernelEventTypes::EventTypeSchema>());

    /// <inheritdoc/>
    public Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default) =>
        Task.FromResult(true);

    /// <inheritdoc/>
    public Task<KernelEventTypes::EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        var eventType = new EventType(type, generation ?? EventTypeGeneration.First);
        return Task.FromResult(new KernelEventTypes::EventTypeSchema(eventType, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema()));
    }
}
