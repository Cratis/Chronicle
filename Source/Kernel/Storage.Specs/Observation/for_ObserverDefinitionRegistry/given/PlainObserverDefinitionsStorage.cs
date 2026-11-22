// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Observation.for_ObserverDefinitionRegistry.given;

/// <summary>
/// An <see cref="IObserverDefinitionsStorage"/> that counts the reads made of it.
/// </summary>
public class PlainObserverDefinitionsStorage : IObserverDefinitionsStorage
{
    readonly Dictionary<ObserverId, ObserverDefinition> _definitions = [];

    public int GetAllCalls { get; private set; }

    public int GetCalls { get; private set; }

    public int HasCalls { get; private set; }

    public int Reads => GetAllCalls + GetCalls + HasCalls;

    /// <summary>
    /// Stores a definition without going through the registry, which is what another silo saving it looks like.
    /// </summary>
    /// <param name="definition">The definition to store.</param>
    public void Seed(ObserverDefinition definition) => _definitions[definition.Identifier] = definition;

    /// <summary>
    /// Removes a definition without going through the registry, which is what another silo deleting it looks like.
    /// </summary>
    /// <param name="id">The observer.</param>
    public void Unseed(ObserverId id) => _definitions.Remove(id);

    public void ResetCounts()
    {
        GetAllCalls = 0;
        GetCalls = 0;
        HasCalls = 0;
    }

    public ISubject<IEnumerable<ObserverDefinition>> ObserveAll() => throw new NotSupportedException();

    public Task<IEnumerable<ObserverDefinition>> GetAll()
    {
        GetAllCalls++;
        return Task.FromResult<IEnumerable<ObserverDefinition>>([.. _definitions.Values]);
    }

    public Task<bool> Has(ObserverId id)
    {
        HasCalls++;
        return Task.FromResult(_definitions.ContainsKey(id));
    }

    public Task<ObserverDefinition> Get(ObserverId id)
    {
        GetCalls++;
        return Task.FromResult(_definitions.TryGetValue(id, out var definition) ? definition : ObserverDefinition.Empty);
    }

    public Task Delete(ObserverId id)
    {
        _definitions.Remove(id);
        return Task.CompletedTask;
    }

    public Task Save(ObserverDefinition definition)
    {
        _definitions[definition.Identifier] = definition;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<ObserverDefinition>> GetForEventTypes(IEnumerable<EventType> eventTypes) =>
        Task.FromResult<IEnumerable<ObserverDefinition>>([.. _definitions.Values.Where(_ => _.EventTypes.Any(type => eventTypes.Any(other => other.Id == type.Id)))]);

    public Task<IEnumerable<ObserverDefinition>> GetReplayableObserversForEventTypes(IEnumerable<EventType> eventTypes) =>
        Task.FromResult<IEnumerable<ObserverDefinition>>([.. _definitions.Values.Where(_ => _.IsReplayable && _.EventTypes.Any(type => eventTypes.Any(other => other.Id == type.Id)))]);
}
