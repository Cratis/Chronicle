// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Storage.InMemory.Observation;

/// <summary>
/// Represents an in-memory implementation of <see cref="IObserverDefinitionsStorage"/>.
/// </summary>
public sealed class ObserverDefinitionsStorage : IObserverDefinitionsStorage, IDisposable
{
    readonly ConcurrentDictionary<ObserverId, ObserverDefinition> _definitions = new();
    readonly ReplaySubject<IEnumerable<ObserverDefinition>> _allSubject = new(1);
    readonly Lock _mutationLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverDefinitionsStorage"/> class.
    /// </summary>
    public ObserverDefinitionsStorage() => _allSubject.OnNext(Snapshot());

    /// <inheritdoc/>
    public ISubject<IEnumerable<ObserverDefinition>> ObserveAll() => _allSubject;

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
        lock (_mutationLock)
        {
            if (_definitions.TryRemove(id, out _))
            {
                _allSubject.OnNext(Snapshot());
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task Save(ObserverDefinition definition)
    {
        lock (_mutationLock)
        {
            _definitions[definition.Identifier] = definition;
            _allSubject.OnNext(Snapshot());
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose() => _allSubject.Dispose();

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

    ObserverDefinition[] Snapshot() => _definitions.Values.ToArray();
}
