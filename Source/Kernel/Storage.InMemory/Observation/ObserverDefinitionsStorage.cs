// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation;

/// <summary>
/// Represents an in-memory implementation of <see cref="IObserverDefinitionsStorage"/>.
/// </summary>
public sealed class ObserverDefinitionsStorage : IObserverDefinitionsStorage
{
    readonly ConcurrentDictionary<ObserverId, ObserverDefinition> _definitions = new();

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverDefinition>> GetAll() =>
        Task.FromResult<IEnumerable<ObserverDefinition>>(_definitions.Values.ToArray());

    /// <inheritdoc/>
    public Task<bool> Has(ObserverId id) =>
        Task.FromResult(_definitions.ContainsKey(id));

    /// <inheritdoc/>
    public Task<ObserverDefinition> Get(ObserverId id) =>
        Task.FromResult(_definitions.TryGetValue(id, out var definition) ? definition : ObserverDefinition.Empty);

    /// <inheritdoc/>
    public Task Delete(ObserverId id)
    {
        _definitions.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Save(ObserverDefinition definition)
    {
        _definitions[definition.Identifier] = definition;
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverDefinition>> GetForEventTypes(IEnumerable<EventType> eventTypes)
    {
        var eventTypeIds = eventTypes.Select(eventType => eventType.Id).ToHashSet();
        var result = _definitions.Values
            .Where(definition => definition.EventTypes.Any(eventType => eventTypeIds.Contains(eventType.Id)))
            .ToArray();
        return Task.FromResult<IEnumerable<ObserverDefinition>>(result);
    }

    /// <inheritdoc/>
    public Task<IEnumerable<ObserverDefinition>> GetReplayableObserversForEventTypes(IEnumerable<EventType> eventTypes)
    {
        var eventTypeIds = eventTypes.Select(eventType => eventType.Id).ToHashSet();
        var result = _definitions.Values
            .Where(definition => definition.IsReplayable && definition.EventTypes.Any(eventType => eventTypeIds.Contains(eventType.Id)))
            .ToArray();
        return Task.FromResult<IEnumerable<ObserverDefinition>>(result);
    }
}
