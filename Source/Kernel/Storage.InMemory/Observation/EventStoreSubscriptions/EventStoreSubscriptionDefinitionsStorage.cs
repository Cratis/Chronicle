// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Observation.EventStoreSubscriptions;
using Cratis.Chronicle.Storage.Observation.EventStoreSubscriptions;

namespace Cratis.Chronicle.Storage.InMemory.Observation.EventStoreSubscriptions;

/// <summary>
/// Represents an in-memory implementation of <see cref="IEventStoreSubscriptionDefinitionsStorage"/>.
/// </summary>
public sealed class EventStoreSubscriptionDefinitionsStorage : IEventStoreSubscriptionDefinitionsStorage
{
    readonly ConcurrentDictionary<EventStoreSubscriptionId, EventStoreSubscriptionDefinition> _definitions = new();

    /// <inheritdoc/>
    public Task<IEnumerable<EventStoreSubscriptionDefinition>> GetAll() =>
        Task.FromResult<IEnumerable<EventStoreSubscriptionDefinition>>(_definitions.Values.ToArray());

    /// <inheritdoc/>
    public Task<bool> Has(EventStoreSubscriptionId id) =>
        Task.FromResult(_definitions.ContainsKey(id));

    /// <inheritdoc/>
    public Task<EventStoreSubscriptionDefinition?> Get(EventStoreSubscriptionId id) =>
        Task.FromResult(_definitions.TryGetValue(id, out var definition) ? definition : null);

    /// <inheritdoc/>
    public Task Delete(EventStoreSubscriptionId id)
    {
        _definitions.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Save(EventStoreSubscriptionDefinition definition)
    {
        _definitions[definition.Identifier] = definition;
        return Task.CompletedTask;
    }
}
