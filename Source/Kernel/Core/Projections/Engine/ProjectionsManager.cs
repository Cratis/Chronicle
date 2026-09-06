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
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine;

/// <summary>
/// Represents the implementation of <see cref="IProjectionsManager"/>.
/// </summary>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="storage"><see cref="IStorage"/> for accessing storage.</param>
[Singleton]
public class ProjectionsManager(IProjectionFactory projectionFactory, IStorage storage) : IProjectionsManager
{
    readonly ConcurrentDictionary<string, RegisteredProjection> _projections = new();

    /// <inheritdoc/>
    public async Task<Result<ProjectionRegistrationError>> Register(EventStoreName eventStore, IEnumerable<ProjectionDefinition> definitions, IEnumerable<ReadModelDefinition> readModelDefinitions, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        var definitionList = definitions.ToList();
        var readModelDefinitionsByIdentifier = readModelDefinitions.ToDictionary(readModel => readModel.Identifier);
        var availableReadModelIdentifiers = string.Join(", ", readModelDefinitionsByIdentifier.Keys.Select(identifier => $"'{identifier.Value}'"));
        var namespaceList = namespaces.ToList();
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var eventTypeSchemas = await eventStoreStorage.EventTypes.GetLatestForAllEventTypes();
        var failures = new ConcurrentDictionary<ProjectionId, Exception>();

        await Task.WhenAll(definitionList.Select(async definition =>
        {
            try
            {
                if (!readModelDefinitionsByIdentifier.TryGetValue(definition.ReadModel, out var readModelDefinition))
                {
                    throw new InvalidOperationException($"ReadModelDefinition with Identifier '{definition.ReadModel.Value}' not found. Available: [{availableReadModelIdentifiers}]");
                }

                var projectionsByNamespace = await Task.WhenAll(namespaceList.Select(async @namespace =>
                {
                    var projection = await projectionFactory.Create(eventStore, @namespace, definition, readModelDefinition, eventTypeSchemas);
                    return KeyValuePair.Create(@namespace, projection);
                }));

                _projections[GetKeyFor(eventStore, definition.Identifier)] = new(
                    definition,
                    new ConcurrentDictionary<EventStoreNamespaceName, IProjection>(projectionsByNamespace));
            }
            catch (Exception exception)
            {
                failures[definition.Identifier] = exception as ProjectionDefinitionRegistrationFailed ??
                    new ProjectionDefinitionRegistrationFailed(definition.Identifier, exception);
            }
        }));

        return failures.IsEmpty
            ? Result<ProjectionRegistrationError>.Success()
            : Result.Failed(new ProjectionRegistrationError(failures));
    }

    /// <inheritdoc/>
    public async Task AddNamespace(EventStoreName eventStore, EventStoreNamespaceName @namespace, IEnumerable<ReadModelDefinition> readModelDefinitions)
    {
        var eventStoreStorage = storage.GetEventStore(eventStore);
        var eventTypeSchemas = await eventStoreStorage.EventTypes.GetLatestForAllEventTypes();
        var readModelDefinitionsByIdentifier = readModelDefinitions.ToDictionary(readModel => readModel.Identifier);
        foreach (var registeredProjection in _projections.Where(keyValuePair => keyValuePair.Key.StartsWith($"{eventStore}{KeyHelper.Separator}")).Select(keyValuePair => keyValuePair.Value))
        {
            var readModel = readModelDefinitionsByIdentifier[registeredProjection.Definition.ReadModel];
            if (!registeredProjection.Projections.ContainsKey(@namespace))
            {
                registeredProjection.Projections[@namespace] = await projectionFactory.Create(eventStore, @namespace, registeredProjection.Definition, readModel, eventTypeSchemas);
            }
        }
    }

    /// <inheritdoc/>
    public bool TryGet(EventStoreName eventStore, EventStoreNamespaceName @namespace, ProjectionId id, [NotNullWhen(true)] out IProjection? projection)
    {
        if (_projections.TryGetValue(GetKeyFor(eventStore, id), out var registeredProjection))
        {
            return registeredProjection.Projections.TryGetValue(@namespace, out projection);
        }

        projection = null;
        return false;
    }

    /// <inheritdoc/>
    public void Evict(EventStoreName eventStore, ProjectionId id) => _projections.TryRemove(GetKeyFor(eventStore, id), out _);

    string GetKeyFor(EventStoreName eventStore, ProjectionId id) => KeyHelper.Combine(eventStore, id);

    sealed record RegisteredProjection(
        ProjectionDefinition Definition,
        ConcurrentDictionary<EventStoreNamespaceName, IProjection> Projections);
}
