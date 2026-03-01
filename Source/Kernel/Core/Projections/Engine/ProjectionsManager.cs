// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents the implementation of <see cref="IProjectionsManager"/>.
/// </summary>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
[Singleton]
public class ProjectionsManager(IProjectionFactory projectionFactory, IStorage storage) : IProjectionsManager
{
    readonly ConcurrentDictionary<string, ProjectionDefinition> _definitions = new();
    readonly ConcurrentDictionary<string, IProjection> _projections = new();

    /// <inheritdoc/>
    public async Task Register(EventStoreName eventStore, IEnumerable<ProjectionDefinition> definitions, IEnumerable<ReadModelDefinition> readModelDefinitions, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        foreach (var definition in definitions)
        {
            var definitionKey = GetKeyFor(eventStore, definition.Identifier);
            _definitions[definitionKey] = definition;
            var readModelDefinition = readModelDefinitions.SingleOrDefault(rm => rm.Identifier == definition.ReadModel);
            if (readModelDefinition is null)
            {
                var availableIdentifiers = string.Join(", ", readModelDefinitions.Select(rm => $"'{rm.Identifier.Value}'"));
                throw new InvalidOperationException($"ReadModelDefinition with Identifier '{definition.ReadModel.Value}' not found. Available: [{availableIdentifiers}]");
            }
            var readModel = readModelDefinition;
            var eventStoreStorage = storage.GetEventStore(eventStore);
            var eventTypeSchemas = await eventStoreStorage.EventTypes.GetLatestForAllEventTypes();
            foreach (var @namespace in namespaces)
            {
                var projection = await projectionFactory.Create(eventStore, @namespace, definition, readModel, eventTypeSchemas);
                var key = $"{eventStore}{KeyHelper.Separator}{@namespace}{KeyHelper.Separator}{definition.Identifier}";
                _projections[key] = projection;
            }
        }
    }

    /// <inheritdoc/>
    public async Task AddNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace, IEnumerable<ReadModelDefinition> readModelDefinitions)
    {
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var eventTypeSchemas = await eventStoreStorage.EventTypes.GetLatestForAllEventTypes();
        foreach (var definition in _definitions.Where(kvp => kvp.Key.StartsWith($"{eventStore}{KeyHelper.Separator}")).Select(kvp => kvp.Value))
        {
            var key = KeyHelper.Combine(eventStore, @namespace, definition.Identifier);
            var readModel = readModelDefinitions.Single(rm => rm.Identifier == definition.ReadModel);
            if (!_projections.ContainsKey(key))
            {
                _projections[key] = await projectionFactory.Create(eventStore, @namespace, definition, readModel, eventTypeSchemas);
            }
        }
    }

    /// <inheritdoc/>
    public bool TryGet(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId id, [NotNullWhen(true)] out IProjection? projection) =>
        _projections.TryGetValue(KeyHelper.Combine(eventStore, @namespace, id), out projection);

    /// <inheritdoc/>
    public void Evict(EventStoreName eventStore, ProjectionId id)
    {
        _definitions.TryRemove(GetKeyFor(eventStore, id), out _);

        foreach (var key in _projections.Keys.Where(k => k.Contains($"{KeyHelper.Separator}{id}")).ToList())
        {
            _projections.TryRemove(key, out _);
        }
    }

    string GetKeyFor(EventStoreName eventStore, ProjectionId id) => KeyHelper.Combine(eventStore, id);
}
