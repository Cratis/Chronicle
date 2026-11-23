// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventTypes;

namespace Cratis.Chronicle.Benchmarks.Storage;

/// <summary>Fixed first-generation schemas; deliberately retains the legacy fixture's empty-schema conversion.</summary>
internal sealed class BenchmarkEventTypes : IEventTypesStorage
{
    readonly Task<EventTypeSchema> _selected = Schema(BenchmarkEvents.Selected);
    readonly Task<EventTypeSchema> _other = Schema(BenchmarkEvents.Other);
    readonly Task<bool> _yes = Task.FromResult(true);
    readonly Task<bool> _no = Task.FromResult(false);

    /// <inheritdoc/>
    public Task<bool> HasFor(EventTypeId type, EventTypeGeneration? generation = default) =>
        IsKnown(type, generation) ? _yes : _no;

    /// <inheritdoc/>
    public Task<EventTypeSchema> GetFor(EventTypeId type, EventTypeGeneration? generation = default)
    {
        if (!IsKnown(type, generation)) throw new NotSupportedException("Only the two benchmark types at first generation are supported.");
        return type == BenchmarkEvents.Selected.Id ? _selected : _other;
    }

    /// <inheritdoc/>
    public void Invalidate(EventTypeId eventTypeId)
    {
    }

    /// <inheritdoc/>
    public Task<EventTypeRegistrationResult> RegisterIfAbsent(EventTypeToRegister eventType) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<bool> ReplaceSchema(EventTypeId eventTypeId, EventTypeGeneration generation, JsonSchema schema) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<bool> TryReplaceSchema(EventTypeId eventTypeId, EventTypeGeneration generation, JsonSchema expected, JsonSchema schema) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<bool> Register(EventType type, JsonSchema schema, EventTypeOwner owner = EventTypeOwner.Client, EventTypeSource source = EventTypeSource.Code, EventTypeVisibility visibility = EventTypeVisibility.Unspecified, string origin = "") => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<bool> Register(EventTypeDefinition definition) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeSchema>> GetLatestForAllEventTypes() => throw new NotSupportedException();

    /// <inheritdoc/>
    public ISubject<IEnumerable<EventTypeSchema>> ObserveLatestForAllEventTypes() => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeDefinition>> GetAllDefinitions() => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<EventTypeDefinition> GetDefinition(EventTypeId eventTypeId) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task RecordMigrationsVersion(EventTypeId eventTypeId, EventTypeMigrationsVersion version, IEnumerable<EventTypeMigrationDefinition> migrations) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeSchema>> GetAllGenerationsForEventType(EventType eventType) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventTypeId> eventTypeIds) => throw new NotSupportedException();

    /// <inheritdoc/>
    public Task<IEnumerable<EventTypeSchema>> GetFor(IEnumerable<EventType> eventTypes) => throw new NotSupportedException();

    static Task<EventTypeSchema> Schema(EventType type) => Task.FromResult(new EventTypeSchema(type, EventTypeOwner.None, EventTypeSource.Unknown, new JsonSchema()));

    static bool IsKnown(EventTypeId type, EventTypeGeneration? generation) =>
        (generation is null || generation == EventTypeGeneration.First) &&
        (type == BenchmarkEvents.Selected.Id || type == BenchmarkEvents.Other.Id);
}
