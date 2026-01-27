// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Grains.Namespaces;
using Cratis.Chronicle.Grains.Observation.States;
using Cratis.Chronicle.Grains.ReadModels;
using Cratis.Chronicle.Grains.Recommendations;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using Orleans.Providers;
using Orleans.Utilities;
using EngineProjection = Cratis.Chronicle.Projections.IProjection;

namespace Cratis.Chronicle.Grains.Projections;

/// <summary>
/// Represents an implementation of <see cref="IProjection"/>.
/// </summary>
/// <param name="projectionDefinitionComparer"><see cref="IProjectionDefinitionComparer"/> for comparing projection definitions.</param>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="storage"><see cref="IStorage"/> for persisting projection state.</param>
/// <param name="logger">Logger for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Projections)]
public class Projection(
    IProjectionDefinitionComparer projectionDefinitionComparer,
    IProjectionFactory projectionFactory,
    IObjectComparer objectComparer,
    IStorage storage,
    ILogger<Projection> logger) : Grain<ProjectionDefinition>, IProjection
{
    readonly ObserverManager<INotifyProjectionDefinitionsChanged> _definitionObservers = new(TimeSpan.FromDays(365 * 4), logger);
    readonly Dictionary<EventStoreNamespaceName, EngineProjection> _projectionsByNamespace = new();
    string _identifierPropertyName = "id";

    /// <inheritdoc/>
    public async Task SetDefinition(ProjectionDefinition definition)
    {
        var key = ProjectionKey.Parse(this.GetPrimaryKeyString());
        logger.SettingDefinition(key.ProjectionId);
        var compareResult = await projectionDefinitionComparer.Compare(key, State, definition);

        State = definition;
        await WriteStateAsync();

        // Cache the identifier property name from the read model schema
        var allReadModels = await storage.GetEventStore(key.EventStore).ReadModels.GetAll();
        var readModelDefinition = allReadModels.FirstOrDefault(r => r.Identifier == State.ReadModel);
        _identifierPropertyName = "id"; // Default

        if (readModelDefinition is not null)
        {
            var schema = readModelDefinition.GetSchemaForLatestGeneration();

            // Check for 'Id' first (PascalCase), then 'id' (camelCase)
            if (schema.Properties.ContainsKey("Id"))
            {
                _identifierPropertyName = "Id";
            }
            else if (schema.Properties.ContainsKey("id"))
            {
                _identifierPropertyName = "id";
            }
        }

        if (compareResult == ProjectionDefinitionCompareResult.Different)
        {
            logger.ProjectionHasChanged(key.ProjectionId);
            _projectionsByNamespace.Clear();
            await _definitionObservers.Notify(notifier => notifier.OnProjectionDefinitionsChanged(definition));
            var namespaceNames = await GrainFactory.GetGrain<INamespaces>(key.EventStore).GetAll();
            await AddReplayRecommendationForAllNamespaces(key, namespaceNames);
        }
    }

    /// <inheritdoc/>
    public Task<ProjectionDefinition> GetDefinition() => Task.FromResult(State);

    /// <inheritdoc/>
    public Task SubscribeDefinitionsChanged(INotifyProjectionDefinitionsChanged subscriber)
    {
        _definitionObservers.Subscribe(subscriber, subscriber);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UnsubscribeDefinitionsChanged(INotifyProjectionDefinitionsChanged subscriber)
    {
        _definitionObservers.Unsubscribe(subscriber);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventType>> GetEventTypes()
    {
        var projection = await GetOrCreateProjectionForNamespace(EventStoreNamespaceName.Default);
        return projection.EventTypes;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<EventType>> GetEventTypesForPreview(ReadModelDefinition readModelDefinition)
    {
        var projection = await GetOrCreateProjectionForNamespaceWithReadModel(EventStoreNamespaceName.Default, readModelDefinition);
        return projection.EventTypes;
    }

    /// <inheritdoc/>
    public async Task<ExpandoObject> ProcessForSingleReadModel(EventStoreNamespaceName eventStoreNamespace, ExpandoObject initialState, IEnumerable<AppendedEvent> events)
    {
        var projectionKey = ProjectionKey.Parse(this.GetPrimaryKeyString());
        var eventStoreNamespaceStorage = storage.GetEventStore(projectionKey.EventStore).GetNamespace(eventStoreNamespace);
        var eventSequenceStorage = eventStoreNamespaceStorage.GetEventSequence(State.EventSequenceId);
        var projection = await GetOrCreateProjectionForNamespace(eventStoreNamespace);
        var state = initialState;
        Key? lastKey = null;

        foreach (var @event in events)
        {
            var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, @event, state);
            var keyResolver = projection!.GetKeyResolverFor(@event.Context.EventType);
            var keyResult = await keyResolver(eventSequenceStorage!, NullSink.Instance, @event);

            // Skip deferred keys in immediate projections - they need parent data that's not yet available
            if (keyResult is DeferredKey)
            {
                continue;
            }

            var key = (keyResult as ResolvedKey)!.Key;
            lastKey = key;
            var context = new ProjectionEventContext(
                key,
                @event,
                changeset,
                projection.GetOperationTypeFor(@event.Context.EventType),
                false);

            await HandleEventFor(projection!, context);

            state = ApplyActualChanges(key, changeset.Changes, state);
        }

        // Inject id property into the read model before returning
        if (lastKey is not null)
        {
            var stateDict = (IDictionary<string, object?>)state;
            stateDict[_identifierPropertyName] = lastKey.Value.ToString();
        }

        return state;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ExpandoObject>> Process(EventStoreNamespaceName eventStoreNamespace, IEnumerable<AppendedEvent> events)
    {
        var projectionKey = ProjectionKey.Parse(this.GetPrimaryKeyString());
        var eventStoreNamespaceStorage = storage.GetEventStore(projectionKey.EventStore).GetNamespace(eventStoreNamespace);
        var eventSequenceStorage = eventStoreNamespaceStorage.GetEventSequence(State.EventSequenceId);
        var projection = await GetOrCreateProjectionForNamespace(eventStoreNamespace);

        // First pass: resolve keys and group events by key value
        var eventsByKeyValue = new Dictionary<string, (Key Key, List<AppendedEvent> Events)>();

        foreach (var @event in events)
        {
            var keyResolver = projection!.GetKeyResolverFor(@event.Context.EventType);
            var keyResult = await keyResolver(eventSequenceStorage!, NullSink.Instance, @event);

            if (keyResult is DeferredKey)
            {
                continue;
            }

            var key = (keyResult as ResolvedKey)!.Key;
            var keyValue = key.Value.ToString()!;

            if (!eventsByKeyValue.TryGetValue(keyValue, out var entry))
            {
                entry = (key, []);
                eventsByKeyValue[keyValue] = entry;
            }

            entry.Events.Add(@event);
        }

        // Second pass: process each group of events for each key
        var readModelsByKeyValue = new Dictionary<string, ExpandoObject>();

        foreach (var (keyValue, (_, eventsForKey)) in eventsByKeyValue)
        {
            var state = new ExpandoObject();

            foreach (var @event in eventsForKey)
            {
                var keyResolver = projection!.GetKeyResolverFor(@event.Context.EventType);
                var keyResult = await keyResolver(eventSequenceStorage!, NullSink.Instance, @event);
                var key = (keyResult as ResolvedKey)!.Key;

                var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, @event, state);
                var context = new ProjectionEventContext(
                    key,
                    @event,
                    changeset,
                    projection.GetOperationTypeFor(@event.Context.EventType),
                    false);

                await HandleEventFor(projection!, context);

                state = ApplyActualChanges(key, changeset.Changes, state);
            }

            readModelsByKeyValue[keyValue] = state;
        }

        // Inject id property into each read model before returning
        var results = new List<ExpandoObject>();
        foreach (var (keyValue, readModel) in readModelsByKeyValue)
        {
            var readModelDict = (IDictionary<string, object?>)readModel;

            // Set the id property with the key value using the schema's property name
            readModelDict[_identifierPropertyName] = keyValue;

            results.Add(readModel);
        }

        return results;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ExpandoObject>> ProcessForPreview(EventStoreNamespaceName eventStoreNamespace, IEnumerable<AppendedEvent> events, ReadModelDefinition readModelDefinition)
    {
        var projectionKey = ProjectionKey.Parse(this.GetPrimaryKeyString());
        var eventStoreNamespaceStorage = storage.GetEventStore(projectionKey.EventStore).GetNamespace(eventStoreNamespace);
        var eventSequenceStorage = eventStoreNamespaceStorage.GetEventSequence(State.EventSequenceId);
        var projection = await GetOrCreateProjectionForNamespaceWithReadModel(eventStoreNamespace, readModelDefinition);

        // Determine the identifier property name from the schema
        var identifierPropertyName = "_id";
        var schema = readModelDefinition.GetSchemaForLatestGeneration();
        if (schema.Properties.ContainsKey("_id"))
        {
            identifierPropertyName = "_id";
        }
        else if (schema.Properties.ContainsKey("id"))
        {
            identifierPropertyName = "id";
        }

        // First pass: resolve keys and group events by key value
        var eventsByKeyValue = new Dictionary<string, (Key Key, List<AppendedEvent> Events)>();

        foreach (var @event in events)
        {
            var keyResolver = projection!.GetKeyResolverFor(@event.Context.EventType);
            var keyResult = await keyResolver(eventSequenceStorage!, NullSink.Instance, @event);

            if (keyResult is DeferredKey)
            {
                continue;
            }

            var key = (keyResult as ResolvedKey)!.Key;
            var keyValue = key.Value.ToString()!;

            if (!eventsByKeyValue.TryGetValue(keyValue, out var entry))
            {
                entry = (key, []);
                eventsByKeyValue[keyValue] = entry;
            }

            entry.Events.Add(@event);
        }

        // Second pass: process each group of events for each key
        var readModelsByKeyValue = new Dictionary<string, ExpandoObject>();

        foreach (var (keyValue, (_, eventsForKey)) in eventsByKeyValue)
        {
            var state = new ExpandoObject();

            foreach (var @event in eventsForKey)
            {
                var keyResolver = projection!.GetKeyResolverFor(@event.Context.EventType);
                var keyResult = await keyResolver(eventSequenceStorage!, NullSink.Instance, @event);
                var key = (keyResult as ResolvedKey)!.Key;

                var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, @event, state);
                var context = new ProjectionEventContext(
                    key,
                    @event,
                    changeset,
                    projection.GetOperationTypeFor(@event.Context.EventType),
                    false);

                await HandleEventFor(projection!, context);

                state = ApplyActualChanges(key, changeset.Changes, state);
            }

            readModelsByKeyValue[keyValue] = state;
        }

        // Inject id property into each read model before returning
        var results = new List<ExpandoObject>();
        foreach (var (keyValue, readModel) in readModelsByKeyValue)
        {
            var readModelDict = (IDictionary<string, object?>)readModel;

            // Set the id property with the key value using the identifier property name
            readModelDict[identifierPropertyName] = keyValue;

            results.Add(readModel);
        }

        return results;
    }

    async Task HandleEventFor(EngineProjection projection, ProjectionEventContext context)
    {
        if (projection.Accepts(context.Event.Context.EventType))
        {
            projection.OnNext(context);
        }

        foreach (var child in projection.ChildProjections)
        {
            await HandleEventFor(child, context);
        }
    }

    ExpandoObject ApplyActualChanges(Key key, IEnumerable<Change> changes, ExpandoObject state)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject> propertiesChanged:
                    state = state.MergeWith((change.State as ExpandoObject)!);
                    break;

                case ChildAdded childAdded:
                    var items = state.EnsureCollection<object>(childAdded.ChildrenProperty, key.ArrayIndexers);
                    items.Add(childAdded.Child);
                    break;

                case Joined joined:
                    state = ApplyActualChanges(key, joined.Changes, state);
                    break;

                case ResolvedJoin resolvedJoin:
                    state = ApplyActualChanges(key, resolvedJoin.Changes, state);
                    break;
            }
        }

        return state;
    }

    async Task<EngineProjection> GetOrCreateProjectionForNamespace(EventStoreNamespaceName eventStoreNamespace)
    {
        if (!_projectionsByNamespace.TryGetValue(eventStoreNamespace, out var projection))
        {
            var key = ProjectionKey.Parse(this.GetPrimaryKeyString());
            var readModelKey = new ReadModelGrainKey(State.ReadModel, key.EventStore);
            var readModel = GrainFactory.GetGrain<IReadModel>(readModelKey);
            var readModelDefinition = await readModel.GetDefinition();
            var eventStoreStorage = storage.GetEventStore(key.EventStore);
            var eventTypeSchemas = await eventStoreStorage.EventTypes.GetLatestForAllEventTypes();

            projection = await projectionFactory.Create(key.EventStore, eventStoreNamespace, State, readModelDefinition, eventTypeSchemas);
            _projectionsByNamespace[eventStoreNamespace] = projection;
        }

        return projection;
    }

    async Task<EngineProjection> GetOrCreateProjectionForNamespaceWithReadModel(EventStoreNamespaceName eventStoreNamespace, ReadModelDefinition readModelDefinition)
    {
        // For preview with provided read model, always create a fresh projection (don't cache)
        var key = ProjectionKey.Parse(this.GetPrimaryKeyString());
        var eventStoreStorage = storage.GetEventStore(key.EventStore);
        var eventTypeSchemas = await eventStoreStorage.EventTypes.GetLatestForAllEventTypes();

        return await projectionFactory.Create(key.EventStore, eventStoreNamespace, State, readModelDefinition, eventTypeSchemas);
    }

    async Task AddReplayRecommendationForAllNamespaces(ProjectionKey key, IEnumerable<EventStoreNamespaceName> namespaces)
    {
        foreach (var @namespace in namespaces)
        {
            var recommendationsManager = GrainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(key.EventStore, @namespace));
            await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
                "Projection definition has changed.",
                new()
                {
                    ObserverId = key.ProjectionId,
                    ObserverKey = new(key.ProjectionId, key.EventStore, @namespace, State.EventSequenceId),
                    Reasons = [new ProjectionDefinitionChangedReplayCandidateReason()]
                });
        }
    }
}
