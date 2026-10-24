// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Replaying;
using Cratis.Chronicle.Concepts.Projections;
using Cratis.Chronicle.Concepts.Projections.Definitions;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Observation.States;
using Cratis.Chronicle.Projections.Engine;
using Cratis.Chronicle.Projections.Engine.Pipelines;
using Cratis.Chronicle.Projections.Engine.Pipelines.Steps;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.Sinks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Orleans.Providers;
using Orleans.Utilities;
using EngineProjection = Cratis.Chronicle.Projections.Engine.IProjection;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="EngineProjection"/>.
/// </summary>
/// <param name="projectionDefinitionComparer"><see cref="IProjectionDefinitionComparer"/> for comparing projection definitions.</param>
/// <param name="projectionFactory"><see cref="IProjectionFactory"/> for creating projections.</param>
/// <param name="objectComparer"><see cref="IObjectComparer"/> for comparing objects.</param>
/// <param name="storage"><see cref="IStorage"/> for persisting projection state.</param>
/// <param name="options"><see cref="IOptions{ChronicleOptions}"/> for accessing Chronicle configuration.</param>
/// <param name="logger">Logger for logging.</param>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Projections)]
public class Projection(
    IProjectionDefinitionComparer projectionDefinitionComparer,
    IProjectionFactory projectionFactory,
    IObjectComparer objectComparer,
    IStorage storage,
    IOptions<ChronicleOptions> options,
    ILogger<Projection> logger) : Grain<ProjectionDefinition>, IProjection
{
    readonly ObserverManager<INotifyProjectionDefinitionsChanged> _definitionObservers = new(TimeSpan.FromDays(365 * 4), logger);
    readonly Dictionary<EventStoreNamespaceName, EngineProjection> _projectionsByNamespace = new();
    string _identifierPropertyName = "id";

    /// <inheritdoc/>
    public async Task SetDefinition(ProjectionDefinition definition)
    {
        if (ProjectionVersions.IsOlder(definition.Version, State.Version))
        {
            logger.IgnoringOlderDefinition(definition.Identifier, definition.Version!, State.Version!);
            return;
        }

        ProjectionGenerationDeliveryValidation.Validate(definition);
        var key = ProjectionKey.Parse(this.GetPrimaryKeyString());
        logger.SettingDefinition(key.ProjectionId);
        var previousDefinition = State;

        // A stale manager or another silo may still hold a legacy snapshot after this grain upgraded.
        // Never let its unknown defaults replace rules already supplied by a lossless registration.
        definition = ProjectionDefinitionNormalization.PreserveKnownStorageFields(definition, previousDefinition);
        var compareResult = await projectionDefinitionComparer.Compare(key, previousDefinition, definition);

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

        var storageUpgraded = previousDefinition.HasLegacyStorageFields && !definition.HasLegacyStorageFields;

        // First writes, including re-writes after removal, must also reach subscribers that activated early.
        if (compareResult != ProjectionDefinitionCompareResult.Same || storageUpgraded)
        {
            _projectionsByNamespace.Clear();
            await _definitionObservers.Notify(notifier => notifier.OnProjectionDefinitionsChanged(definition));
        }

        if (compareResult == ProjectionDefinitionCompareResult.Different)
        {
            logger.ProjectionHasChanged(key.ProjectionId);
            var namespaceNames = (await GrainFactory.GetGrain<INamespaces>(key.EventStore).GetAll()).ToList();

            await EvolveForAllNamespaces(key, namespaceNames, previousDefinition, definition);
        }
    }

    /// <inheritdoc/>
    public Task<ProjectionDefinition> GetDefinition() => Task.FromResult(State);

    /// <inheritdoc/>
    public async Task Remove()
    {
        _projectionsByNamespace.Clear();
        await ClearStateAsync();
        DeactivateOnIdle();
    }

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
    public Task<ExpandoObject> ProcessForSingleReadModel(EventStoreNamespaceName eventStoreNamespace, ExpandoObject initialState, IEnumerable<AppendedEvent> events) =>
        DiscardingTheEngineOnFailure(eventStoreNamespace, () => ProjectForSingleReadModel(eventStoreNamespace, initialState, events));

    /// <inheritdoc/>
    public Task<SingleReadModelProjectionState> FoldForSingleReadModel(EventStoreNamespaceName eventStoreNamespace, SingleReadModelProjectionState initialState, IEnumerable<AppendedEvent> events) =>
        DiscardingTheEngineOnFailure(eventStoreNamespace, () => FoldSingleReadModel(eventStoreNamespace, initialState, events));

    /// <inheritdoc/>
    public Task<IEnumerable<ExpandoObject>> Process(EventStoreNamespaceName eventStoreNamespace, IEnumerable<AppendedEvent> events) =>
        DiscardingTheEngineOnFailure(eventStoreNamespace, () => Project(eventStoreNamespace, events));

    /// <inheritdoc/>
    public async Task<IEnumerable<AppendedEvent>> GetEventsForKey(EventStoreNamespaceName eventStoreNamespace, ReadModelKey key, IEnumerable<AppendedEvent> events)
    {
        var projectionKey = ProjectionKey.Parse(this.GetPrimaryKeyString());
        var eventSequenceStorage = storage
            .GetEventStore(projectionKey.EventStore)
            .GetNamespace(eventStoreNamespace)
            .GetEventSequence(State.EventSequenceId);
        var projection = await GetOrCreateProjectionForNamespace(eventStoreNamespace);

        var wanted = key.Value;
        var result = new List<AppendedEvent>();

        foreach (var @event in events)
        {
            var keyResolver = projection.GetKeyResolverFor(@event.Context.EventType);
            var keyResult = await keyResolver(eventSequenceStorage, NullSink.Instance, @event);

            // A deferred key cannot be resolved without the state the projection is building, so it
            // cannot be attributed to an instance here.
            if (keyResult is not ResolvedKey resolved)
            {
                continue;
            }

            if (string.Equals(resolved.Key.Value.ToString(), wanted, StringComparison.Ordinal))
            {
                result.Add(@event);
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ExpandoObject>> ProcessForPreview(EventStoreNamespaceName eventStoreNamespace, IEnumerable<AppendedEvent> events, ReadModelDefinition readModelDefinition)
    {
        var projectionKey = ProjectionKey.Parse(this.GetPrimaryKeyString());
        var eventSequenceStorage = storage.GetEventStore(projectionKey.EventStore).GetNamespace(eventStoreNamespace).GetEventSequence(State.EventSequenceId);
        var projection = await GetOrCreateProjectionForNamespaceWithReadModel(eventStoreNamespace, readModelDefinition);

        // Determine the identifier property name from the schema
        var schemaProperties = readModelDefinition.GetSchemaForLatestGeneration().Properties;
        var identifierPropertyName = !schemaProperties.ContainsKey("_id") && schemaProperties.ContainsKey("id") ? "id" : "_id";

        var sink = await ProjectInMemory(projection, eventSequenceStorage, events);
        return [.. sink.ReadModels.Select(readModel => WithIdentifier(readModel, identifierPropertyName))];
    }

    static bool SuppressesDirectRootUpdate(Change[] changes) =>
        changes.OfType<Joined>().Any(joined => joined.ArrayIndexers.IsEmpty) &&
        !changes.OfType<Joined>().Any(joined => joined.HasKeyedFrom) &&
        !changes.Any(change => change is ChildAdded or ChildRemoved);

    static bool CreatesInstance(EngineProjection projection, ProjectionEventContext context) =>
        projection.Accepts(context.Event.Context.EventType) && context.CreatesInstance;

    /// <summary>
    /// Drops the initialization marker from the changes: it is bookkeeping, and on its own it must not bring an instance into existence.
    /// </summary>
    /// <param name="changes">The changes recorded for the event.</param>
    /// <returns>The changes without any that only record the marker.</returns>
    static Change[] WithoutInitializationMarker(Change[] changes) =>
        [.. changes.Where(change => change is not PropertiesChanged<ExpandoObject> properties ||
            !properties.Differences.All(difference => difference.PropertyPath == WellKnownProperties.ReadModelInstanceInitialized))];

    static bool ShouldMaterializeReadModel(
        bool isMaterialized,
        Key key,
        Change[] changes,
        bool createsInstance)
    {
        if (changes.Any(change => change is Removed))
        {
            return false;
        }

        if (isMaterialized)
        {
            return true;
        }

        // The materializing pipeline writes the instance for an event that creates it even when the event changes
        // nothing, so an event without properties still brings its read model into existence.
        if (changes.Length == 0)
        {
            return createsInstance;
        }

        var hasDirectKeyScopedChanges = changes.Any(change =>
            change is PropertiesChanged<ExpandoObject> or ChildAdded or ChildRemoved);
        var hasConstructiveChanges = changes.Any(change =>
            change is ChildAdded or ChildRemoved);
        var hasJoined = changes.Any(change => change is Joined or ChildRemovedFromAll);
        var onlyPropertyUpdatesAlongsideJoin = hasJoined && hasDirectKeyScopedChanges && !hasConstructiveChanges;
        var isRootLevelJoin = hasJoined && !key.ArrayIndexers.All.Any();

        return !onlyPropertyUpdatesAlongsideJoin && !isRootLevelJoin;
    }

    static void PrepareSingleReadModelSink(PreviewSink sink, Key key, AppendedEvent @event, SingleReadModelProjectionState state)
    {
        // SetInitialState may fill a placeholder's missing properties in place. Keep that working
        // state separate: only its recorded changes reach the instance, just as in a materialized sink.
        if (state.Exists)
        {
            var workingState = state.Model.Clone();
            ((IDictionary<string, object?>)workingState)[WellKnownProperties.ReadModelInstanceInitialized] = state.IsInitialized;
            sink.Set(key.Value.ToString()!, workingState, @event.Context.SequenceNumber);
        }
        else
        {
            sink.Remove(key.Value.ToString()!);
        }
    }

    static void SeedAtCreation(EngineProjection projection, ProjectionEventContext context)
    {
        if (context.NeedsInitialState && !context.Changeset.HasJoined())
        {
            context.Changeset.AddPropertiesFrom(projection.InitialModelState, projection.GetChildrenPropertyPaths());
        }
    }

    static ExpandoObject WithIdentifier(PreviewReadModel readModel, string identifierPropertyName)
    {
        ((IDictionary<string, object?>)readModel.State)[identifierPropertyName] = readModel.KeyValue;
        return readModel.State;
    }

    async Task<ExpandoObject> ProjectForSingleReadModel(EventStoreNamespaceName eventStoreNamespace, ExpandoObject initialState, IEnumerable<AppendedEvent> events)
    {
        var projectionKey = ProjectionKey.Parse(this.GetPrimaryKeyString());
        var eventStoreNamespaceStorage = storage.GetEventStore(projectionKey.EventStore).GetNamespace(eventStoreNamespace);
        var eventSequenceStorage = eventStoreNamespaceStorage.GetEventSequence(State.EventSequenceId);
        var projection = await GetOrCreateProjectionForNamespace(eventStoreNamespace);
        var state = initialState;
        Key? lastKey = null;
        var hasReadModel = ((IDictionary<string, object?>)state).Count > 0;

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
            var operationType = projection.GetOperationTypeFor(@event.Context.EventType);
            var context = new ProjectionEventContext(
                key,
                @event,
                changeset,
                operationType,
                false);

            await HandleEventFor(projection!, context, NullSink.Instance, eventSequenceStorage);

            var shouldMaterialize = ShouldMaterializeReadModel(hasReadModel, key, [.. changeset.Changes], CreatesInstance(projection!, context));
            if (changeset.HasBeenRemoved())
            {
                state = new ExpandoObject();
                lastKey = null;
            }
            else if (shouldMaterialize)
            {
                if (changeset.HasChanges)
                {
                    state = InMemoryChanges.Apply(key, changeset.Changes, state);
                }

                ReadModelSubjects.TrackChanges(state, changeset.Changes, @event);
                lastKey = key;
            }

            hasReadModel = shouldMaterialize;
        }

        // Inject id property into the read model before returning
        if (hasReadModel && lastKey is not null)
        {
            var stateDict = (IDictionary<string, object?>)state;
            stateDict[_identifierPropertyName] = lastKey.Value.ToString();
        }

        return state;
    }

    async Task<SingleReadModelProjectionState> FoldSingleReadModel(EventStoreNamespaceName eventStoreNamespace, SingleReadModelProjectionState initialState, IEnumerable<AppendedEvent> events)
    {
        var projectionKey = ProjectionKey.Parse(this.GetPrimaryKeyString());
        var eventSequenceStorage = storage.GetEventStore(projectionKey.EventStore).GetNamespace(eventStoreNamespace).GetEventSequence(State.EventSequenceId);
        var projection = await GetOrCreateProjectionForNamespace(eventStoreNamespace);
        var sink = new PreviewSink(projection.IdentifiedByProperty ?? new PropertyPath(_identifierPropertyName));
        var initialize = new SetInitialState(sink, NullLogger<SetInitialState>.Instance);
        var result = initialState.Exists ? initialState with { Model = initialState.Model.Clone() } : SingleReadModelProjectionState.Empty;

        foreach (var @event in events)
        {
            var keyResult = await projection.GetKeyResolverFor(@event.Context.EventType)(eventSequenceStorage, NullSink.Instance, @event);
            if (keyResult is not ResolvedKey resolved)
            {
                continue;
            }

            var key = resolved.Key;

            PrepareSingleReadModelSink(sink, key, @event, result);
            var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, @event, result.Model);
            var context = new ProjectionEventContext(key, @event, changeset, projection.GetOperationTypeFor(@event.Context.EventType), false);
            context = await initialize.Perform(projection, context);
            await HandleEventFor(projection, context, NullSink.Instance, eventSequenceStorage);
            SeedAtCreation(projection, context);
            result = ApplySingleReadModelChanges(projection, context, result);
        }

        return result;
    }

    SingleReadModelProjectionState ApplySingleReadModelChanges(EngineProjection projection, ProjectionEventContext context, SingleReadModelProjectionState state)
    {
        var changeset = context.Changeset;
        if (changeset.HasBeenRemoved())
        {
            return SingleReadModelProjectionState.Empty;
        }

        if (!ShouldMaterializeReadModel(state.Exists, context.Key, [.. changeset.Changes], CreatesInstance(projection, context)))
        {
            return state;
        }

        var model = InMemoryChanges.Apply(context.Key, changeset.Changes, state.Model);
        ((IDictionary<string, object?>)model)[_identifierPropertyName] = context.Key.Value.ToString();
        ReadModelSubjects.TrackChanges(model, changeset.Changes, context.Event);
        return new(model, true, state.IsInitialized || CreatesInstance(projection, context));
    }

    async Task<IEnumerable<ExpandoObject>> Project(EventStoreNamespaceName eventStoreNamespace, IEnumerable<AppendedEvent> events)
    {
        var projectionKey = ProjectionKey.Parse(this.GetPrimaryKeyString());
        var eventSequenceStorage = storage.GetEventStore(projectionKey.EventStore).GetNamespace(eventStoreNamespace).GetEventSequence(State.EventSequenceId);
        var projection = await GetOrCreateProjectionForNamespace(eventStoreNamespace);

        var sink = await ProjectInMemory(projection, eventSequenceStorage, events);

        // Set the last handled event sequence number to mirror what the sink writes
        return [.. sink.ReadModels.Select(readModel =>
        {
            var state = WithIdentifier(readModel, _identifierPropertyName);
            ((IDictionary<string, object?>)state)[WellKnownProperties.LastHandledEventSequenceNumber] = (ulong)readModel.LastHandledEventSequenceNumber;
            return state;
        })];
    }

    /// <summary>
    /// Projects events into read models held in memory, in the order the events happened.
    /// </summary>
    /// <remarks>
    /// Keys resolve against the read models built so far, the way they resolve against the sink of a saved projection,
    /// so a join event reaches the read models holding the child it refers to and a keyed update finds the child it
    /// updates.
    /// </remarks>
    /// <param name="projection">The projection to project with.</param>
    /// <param name="eventSequenceStorage">The storage of the event sequence the events come from.</param>
    /// <param name="events">The events to project.</param>
    /// <returns>The <see cref="PreviewSink"/> holding the read models built.</returns>
    async Task<PreviewSink> ProjectInMemory(EngineProjection projection, IEventSequenceStorage eventSequenceStorage, IEnumerable<AppendedEvent> events)
    {
        var sink = new PreviewSink(projection.IdentifiedByProperty ?? new PropertyPath(_identifierPropertyName));
        var initialize = new SetInitialState(sink, NullLogger<SetInitialState>.Instance);
        foreach (var @event in events)
        {
            var keyResult = await projection.GetKeyResolverFor(@event.Context.EventType)(eventSequenceStorage, sink, @event);
            if (keyResult is ResolvedKey resolvedKey)
            {
                await ProjectInMemory(projection, eventSequenceStorage, sink, initialize, resolvedKey, @event);
            }
        }

        return sink;
    }

    async Task ProjectInMemory(EngineProjection projection, IEventSequenceStorage eventSequenceStorage, PreviewSink sink, SetInitialState initialize, ResolvedKey resolvedKey, AppendedEvent @event)
    {
        var key = resolvedKey.Key;
        var keyValue = key.Value.ToString()!;
        var initialState = sink.GetInitialState(key);
        var changeset = new Changeset<AppendedEvent, ExpandoObject>(objectComparer, @event, initialState);
        var context = new ProjectionEventContext(key, @event, changeset, projection.GetOperationTypeFor(@event.Context.EventType), false, resolvedKey.JoinKey);

        context = await initialize.Perform(projection, context);
        await HandleEventFor(projection, context, sink, eventSequenceStorage);
        SeedAtCreation(projection, context);

        if (changeset.HasBeenRemoved())
        {
            sink.Remove(keyValue);
            return;
        }

        var originalChanges = changeset.Changes.ToArray();
        var appliedJoins = sink.ApplyJoinsOnRoots(originalChanges, @event)
            .Concat(sink.ApplyJoinsOnChildren(originalChanges, @event)).ToArray();
        var hasReadModel = sink.TryGet(keyValue, out var state);
        if (!ShouldMaterializeReadModel(hasReadModel, key, WithoutInitializationMarker(originalChanges), CreatesInstance(projection, context)) || SuppressesDirectRootUpdate(originalChanges))
        {
            return;
        }

        var changes = originalChanges.Where(change => !appliedJoins.Any(joined => ReferenceEquals(joined, change))).ToArray();
        if (changes.Length > 0 || (!hasReadModel && CreatesInstance(projection, context)))
        {
            state = InMemoryChanges.Apply(key, changes, state);
            sink.Set(keyValue, state, @event.Context.SequenceNumber);
        }

        ReadModelSubjects.TrackChanges(state, changes, @event);
    }

    async Task HandleEventFor(EngineProjection projection, ProjectionEventContext context, ISink sink, IEventSequenceStorage eventSequenceStorage)
    {
        context = context with { Key = InMemoryChildKeys.Bind(context.Key, context.Changeset.InitialState) };
        if (projection.Accepts(context.Event.Context.EventType))
        {
            projection.OnNext(context);
        }

        foreach (var child in projection.ChildProjections)
        {
            // For child projections, resolve the child key (which includes ArrayIndexers for the
            // children property path). Without this, the child projection's Project subscriber
            // returns early because context.Key.ArrayIndexers.HasFor(childrenProperty) is false.
            if (child.Accepts(context.Event.Context.EventType))
            {
                var childKeyResolver = child.GetKeyResolverFor(context.Event.Context.EventType);
                var childKeyResult = await childKeyResolver(eventSequenceStorage, sink, context.Event);
                if (childKeyResult is ResolvedKey childResolvedKey)
                {
                    var childContext = context with
                    {
                        Key = childResolvedKey.Key,
                        OperationType = child.GetOperationTypeFor(context.Event.Context.EventType)
                    };
                    await HandleEventFor(child, childContext, sink, eventSequenceStorage);
                    continue;
                }
            }

            await HandleEventFor(child, context, sink, eventSequenceStorage);
        }
    }

    /// <summary>
    /// Runs projection work, discarding the namespace's projection engine when it fails.
    /// </summary>
    /// <remarks>
    /// The engine is cached and shared by every projection this grain does for the namespace, and it dispatches
    /// events through Rx subscriptions that dispose themselves when a handler throws. An engine that has thrown
    /// once therefore keeps running but projects nothing through the subscription that threw - every later read
    /// of every instance comes back without a read model, and nothing reports it (Cratis/Chronicle#4337). The
    /// failing call still fails; the next one starts from a fresh engine.
    /// </remarks>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="eventStoreNamespace">The namespace the work projects for.</param>
    /// <param name="work">The work.</param>
    /// <returns>The result of the work.</returns>
    async Task<TResult> DiscardingTheEngineOnFailure<TResult>(EventStoreNamespaceName eventStoreNamespace, Func<Task<TResult>> work)
    {
        try
        {
            return await work();
        }
        catch (Exception exception)
        {
            logger.DiscardingFailedProjectionEngine(ProjectionKey.Parse(this.GetPrimaryKeyString()).ProjectionId, eventStoreNamespace, exception);
            if (_projectionsByNamespace.Remove(eventStoreNamespace, out var engine) && engine is IDisposable disposable)
            {
                disposable.Dispose();
            }

            throw;
        }
    }

    async Task<EngineProjection> GetOrCreateProjectionForNamespace(EventStoreNamespaceName eventStoreNamespace)
    {
        if (!_projectionsByNamespace.TryGetValue(eventStoreNamespace, out var projection))
        {
            var key = ProjectionKey.Parse(this.GetPrimaryKeyString());

            if (State.ReadModel is null)
            {
                throw new ProjectionDefinitionNotSet(key);
            }

            var readModelKey = new ReadModelGrainKey(State.ReadModel, key.EventStore);
            var readModel = GrainFactory.GetGrain<IReadModel>(readModelKey);
            var readModelDefinition = await readModel.GetDefinition();
            var eventStoreStorage = storage.GetEventStore(key.EventStore);
            var eventTypeSchemas = await eventStoreStorage.EventTypes.GetLatestForAllEventTypes();

            // Direct computation belongs to the immediate/read lane, not materialized observer delivery.
            projection = await projectionFactory.Create(key.EventStore, eventStoreNamespace, State with { GenerationDelivery = EventGenerationDelivery.Compatibility }, readModelDefinition, eventTypeSchemas);
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

        return await projectionFactory.Create(key.EventStore, eventStoreNamespace, State with { GenerationDelivery = EventGenerationDelivery.Compatibility }, readModelDefinition, eventTypeSchemas);
    }

    async Task EvolveForAllNamespaces(
        ProjectionKey key,
        IEnumerable<EventStoreNamespaceName> namespaces,
        ProjectionDefinition previousDefinition,
        ProjectionDefinition currentDefinition)
    {
        var namespacePlans = new List<(EventStoreNamespaceName Namespace, DefinitionEvolutionPlan Plan)>();
        var addedEventTypes = ProjectionReplayRecommendationEvaluator.GetAddedEventTypesIfOnlyEventTypesChanged(
            previousDefinition,
            currentDefinition,
            objectComparer);

        foreach (var @namespace in namespaces)
        {
            var plan = DefinitionEvolutionPlan.FullReplay;
            if (addedEventTypes.Length > 0)
            {
                var eventSequence = storage.GetEventStore(key.EventStore).GetNamespace(@namespace).GetEventSequence(currentDefinition.EventSequenceId);

                // Historical writes into the live container can be rejected by its watermark or applied twice.
                // Definition evolution must rebuild the projection instead of replaying selected mappings in place.
                plan = await DefinitionEvolutionPlan.ForAddedEventTypes(
                    eventSequence,
                    addedEventTypes,
                    canReplayPerEventSource: false);
            }
            namespacePlans.Add((@namespace, plan));
        }

        var policy = options.Value.Observers.DefinitionEvolution;
        foreach (var (@namespace, plan) in namespacePlans)
        {
            switch (plan.Operation)
            {
                case DefinitionEvolutionOperation.NoAction:
                    logger.ProjectionEvolutionNeedsNoAction(key.ProjectionId, @namespace);
                    break;
                case DefinitionEvolutionOperation.FullReplay when policy.Allows(plan.Operation) && currentDefinition.Scope != ProjectionScope.Global:
                    this.ScheduleInSeparateTurn(() => ReplayProjection(key, @namespace));
                    break;
                default:
                    await AddReplayRecommendation(key, @namespace, currentDefinition);
                    break;
            }
        }
    }

    async Task ReplayProjection(ProjectionKey key, EventStoreNamespaceName @namespace)
    {
        logger.AutoReplayingProjection(key.ProjectionId, @namespace);
        var observer = GrainFactory.GetGrain<IObserver>(new ObserverKey(key.ProjectionId, key.EventStore, @namespace, State.EventSequenceId));
        await observer.Replay();
    }

    async Task AddReplayRecommendation(
        ProjectionKey key,
        EventStoreNamespaceName @namespace,
        ProjectionDefinition currentDefinition)
    {
        var readModelDefinitions = storage.GetEventStore(key.EventStore).ReadModels;
        var readModelDefinition = await readModelDefinitions.Get(currentDefinition.ReadModel);
        var readModelMigrationRecommendation = ProjectionReplayRecommendationEvaluator.GetReadModelMigrationRecommendation(readModelDefinition);

        (string Description, ReplayCandidateReason Reason) replayRecommendation = readModelMigrationRecommendation switch
        {
            _ when currentDefinition.Scope == ProjectionScope.Global => (
                GlobalProjectionReplay.RefusalReason,
                new ProjectionDefinitionChangedReplayCandidateReason()),
            ProjectionReadModelMigrationRecommendation.UpdateAvailable => (
                "Projection definition has changed. Update available.",
                new ProjectionUpdateAvailableReplayCandidateReason()),
            ProjectionReadModelMigrationRecommendation.SelectiveReplayAvailable => (
                "Projection definition has changed. Selective replay available.",
                new ProjectionSelectiveReplayAvailableReplayCandidateReason()),
            _ => (
                "Projection definition has changed.",
                new ProjectionDefinitionChangedReplayCandidateReason())
        };

        var recommendationsManager = GrainFactory.GetGrain<IRecommendationsManager>(0, new RecommendationsManagerKey(key.EventStore, @namespace));
        await recommendationsManager.Add<IReplayCandidateRecommendation, ReplayCandidateRequest>(
            replayRecommendation.Description,
            new()
            {
                ObserverId = key.ProjectionId,
                ObserverKey = new(key.ProjectionId, key.EventStore, @namespace, State.EventSequenceId),
                Reasons = [replayRecommendation.Reason]
            });
    }
}
