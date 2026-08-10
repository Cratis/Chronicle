// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Dynamic;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using Cratis.Json;
using Cratis.Serialization;
using Microsoft.Extensions.Logging;
using FrameworkNullLoggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory;
using InMemoryEventSequenceStorage = Cratis.Chronicle.Storage.InMemory.EventSequences.EventSequenceStorage;
using KernelAppendedEvent = KernelConcepts::Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelConceptsNs = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelEventTypes = KernelConcepts::Cratis.Chronicle.Concepts.EventTypes;
using KernelKey = KernelConcepts::Cratis.Chronicle.Concepts.Keys.Key;
using KernelProjectionEngine = KernelCore::Cratis.Chronicle.Projections.Engine;
using KernelReadModels = KernelConcepts::Cratis.Chronicle.Concepts.ReadModels;
using KernelSinks = KernelConcepts::Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Processes events through a projection engine to produce a read model instance for testing.
/// </summary>
internal static class ProjectionReadModelProcessor
{
    /// <summary>
    /// The options a materialized read model is deserialized with.
    /// </summary>
    /// <remarks>
    /// The same read-model collection semantics the client reader applies, so that what a spec sees and what a
    /// running system sees are produced by one piece of code rather than by two that happen to agree. Whichever
    /// way an absent collection is resolved, both tiers inherit the answer.
    /// </remarks>
    static readonly JsonSerializerOptions _readModelSerializerOptions =
        new JsonSerializerOptions(Globals.JsonSerializerOptions).WithDeclaredCollectionsNeverNull();

    static readonly IObjectComparer _objectComparer;
    static readonly TypeFormats _typeFormats;
    static readonly KernelProjectionEngine::Expressions.EventValues.EventValueProviderExpressionResolvers _eventValueProviderExpressionResolvers;
    static readonly KernelProjectionEngine::KeyResolvers _keyResolvers;
    static readonly KernelProjectionEngine::Expressions.ReadModelPropertyExpressionResolvers _readModelPropertyExpressionResolvers;
    static readonly KernelProjectionEngine::Expressions.Keys.KeyExpressionResolvers _keyExpressionResolvers;
    static readonly ExpandoObjectConverter _expandoObjectConverter;

    static ProjectionReadModelProcessor()
    {
        var nullLoggerFactory = FrameworkNullLoggerFactory.Instance;

        _typeFormats = new TypeFormats();

        _eventValueProviderExpressionResolvers = new KernelProjectionEngine::Expressions.EventValues.EventValueProviderExpressionResolvers(
            _typeFormats,
            nullLoggerFactory.CreateLogger<KernelProjectionEngine::Expressions.EventValues.EventValueProviderExpressionResolvers>());

        _keyResolvers = new KernelProjectionEngine::KeyResolvers(
            nullLoggerFactory.CreateLogger<KernelProjectionEngine::KeyResolvers>());

        _readModelPropertyExpressionResolvers = new KernelProjectionEngine::Expressions.ReadModelPropertyExpressionResolvers(
            _eventValueProviderExpressionResolvers,
            _typeFormats,
            nullLoggerFactory.CreateLogger<KernelProjectionEngine::Expressions.ReadModelPropertyExpressionResolvers>());

        _keyExpressionResolvers = new KernelProjectionEngine::Expressions.Keys.KeyExpressionResolvers(
            _eventValueProviderExpressionResolvers,
            _keyResolvers,
            nullLoggerFactory.CreateLogger<KernelProjectionEngine::Expressions.Keys.KeyExpressionResolvers>());

        _expandoObjectConverter = new ExpandoObjectConverter(_typeFormats);
        _objectComparer = new ObjectComparer();
    }

    /// <summary>
    /// Processes the given events through the projection for <typeparamref name="TReadModel"/> and returns the resulting read model.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model produced by the projection.</typeparam>
    /// <param name="projectionDefinition">The client-side <see cref="Contracts.Projections.ProjectionDefinition"/>.</param>
    /// <param name="events">The events with their associated <see cref="EventSourceId"/> to process.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for looking up event type metadata.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> for building the read model schema.</param>
    /// <param name="initialState">Optional initial read model state.</param>
    /// <param name="strictEventSubscription">
    /// When <see langword="false"/> (the default), a seeded event the projection does not subscribe to is
    /// silently skipped, matching the production engine. When <see langword="true"/>, such an event raises
    /// <see cref="UnsubscribedEventSeeded"/> instead.
    /// </param>
    /// <returns>
    /// A tuple of the primary projected read model (the threaded result exposed as <c>Instance</c>, or
    /// <see langword="null"/> if the projection did not apply any changes) and a dictionary of every
    /// materialized instance keyed by its event source id (read per-key from the sink, so a multi-source
    /// projection such as a join can be asserted against the intended instance deterministically).
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when a deferred key cannot be resolved after retrying all events.</exception>
    /// <exception cref="UnsubscribedEventSeeded">Thrown when strict mode is enabled and a seeded event is not subscribed to.</exception>
    public static async Task<(TReadModel? Primary, IReadOnlyDictionary<EventSourceId, TReadModel> Instances)> Process<TReadModel>(
        Contracts.Projections.ProjectionDefinition projectionDefinition,
        IEnumerable<(EventSourceId EventSourceId, object Event)> events,
        IEventTypes eventTypes,
        IJsonSchemaGenerator jsonSchemaGenerator,
        TReadModel? initialState = null,
        bool strictEventSubscription = false)
        where TReadModel : class
    {
        var readModelType = typeof(TReadModel);
        var schema = jsonSchemaGenerator.Generate(readModelType);

        var kernelReadModelDefinition = BuildKernelReadModelDefinition(readModelType, schema);
        var kernelProjectionDefinition = KernelCore::Cratis.Chronicle.Services.Projections.Definitions.ProjectionDefinitionConverters.ToChronicle(
            projectionDefinition,
            KernelConceptsNs::Projections.ProjectionOwner.Client);

        var eventsList = events.ToList();

        // Build AppendedEvents with correct EventSourceIds for use in key resolution
        var appendedEvents = eventsList
            .Select((eventTuple, index) =>
            {
                var clientEventType = eventTypes.GetEventTypeFor(eventTuple.Event.GetType());
                var kernelEventType = ToKernelEventType(clientEventType);
                var content = eventTuple.Event.AsExpandoObject(true);
                var eventSourceId = (KernelConceptsNs::Events.EventSourceId)eventTuple.EventSourceId.Value;
                var context = KernelConceptsNs::Events.EventContext.Empty with
                {
                    EventType = kernelEventType,
                    EventSourceId = eventSourceId,
                    SequenceNumber = (KernelConceptsNs::Events.EventSequenceNumber)(uint)index,

                    // Give each event a distinct, monotonically increasing occurred time so time-based
                    // projections (e.g. a [FromAll] "last updated" mapped from EventContext.Occurred)
                    // reflect append order the same way the real runtime does — rather than every event
                    // sharing one timestamp.
                    Occurred = KernelConceptsNs::Events.EventContext.Empty.Occurred.AddTicks(index)
                };
                return new KernelAppendedEvent(context, content);
            })
            .ToArray();

        // Populate in-memory event sequence storage with all events so key resolvers
        // (e.g. FromParentHierarchy for ChildrenFrom projections) can look up parent events
        // across separate event source streams.
        var eventSequenceId = KernelConceptsNs::EventSequences.EventSequenceId.Log;
        var inMemoryEventSequenceStorage = new InMemoryEventSequenceStorage(
            KernelConceptsNs::EventStoreName.NotSet,
            KernelConceptsNs::EventStoreNamespaceName.NotSet,
            eventSequenceId);
        foreach (var appendedEvent in appendedEvents)
        {
            await inMemoryEventSequenceStorage.Append(
                appendedEvent.Context.SequenceNumber,
                appendedEvent.Context.EventSourceType,
                appendedEvent.Context.EventSourceId,
                appendedEvent.Context.EventStreamType,
                appendedEvent.Context.EventStreamId,
                appendedEvent.Context.EventType,
                appendedEvent.Context.CorrelationId,
                appendedEvent.Context.Causation,
                [],
                appendedEvent.Context.Tags,
                appendedEvent.Context.Occurred,
                new Dictionary<KernelConceptsNs::Events.EventTypeGeneration, ExpandoObject>
                {
                    { KernelConceptsNs::Events.EventTypeGeneration.First, appendedEvent.Content }
                },
                new Dictionary<KernelConceptsNs::Events.EventTypeGeneration, KernelConceptsNs::Events.EventHash>());
        }

        // Generate event type schemas so AutoMap can map properties from events to read model fields.
        var eventTypeSchemas = BuildEventTypeSchemas(eventsList, eventTypes, jsonSchemaGenerator);

        // Create a projection factory backed by the populated in-memory storage so that
        // join and parent-hierarchy resolvers can look up events from all event streams.
        var storageForFactory = new InMemoryStorage(inMemoryEventSequenceStorage);
        var projectionFactory = CreateProjectionFactory(storageForFactory);

        var engineProjection = await projectionFactory.Create(
            KernelConceptsNs::EventStoreName.NotSet,
            KernelConceptsNs::EventStoreNamespaceName.NotSet,
            kernelProjectionDefinition,
            kernelReadModelDefinition,
            eventTypeSchemas);

        var state = initialState is not null
            ? initialState.AsExpandoObject(true)
            : CreateInitialStateFromSchema(schema, engineProjection);

        // Capture the first non-deferred key resolved for the root projection. In production, MongoDB
        // upserts each read model document with `_id` = this key value, and the BSON deserializer maps
        // `_id` back onto the read model's identifier property (the property marked [Key] / [Subject]
        // or conventionally named "Id"). The in-memory test harness skips the MongoDB round-trip, so
        // we mirror that mapping ourselves before serializing the state — otherwise identifier
        // properties whose only source is the event-source ID surface as null in _scenario.Instance.
        KernelKey? rootKey = null;
        var explicitInitialStateIsPresent = initialState is not null;

        using var inMemorySink = new InMemorySink(kernelReadModelDefinition, _typeFormats);

        // Process events, retrying any that return DeferredKey once all other events have been processed.
        var deferredEvents = new Queue<KernelAppendedEvent>();
        foreach (var @event in appendedEvents)
        {
            var (newState, eventKey, eventRemoved) = await ProcessSingleEvent(engineProjection, kernelProjectionDefinition, inMemoryEventSequenceStorage, inMemorySink, @event, state, deferredEvents, strictEventSubscription);
            state = newState;
            rootKey ??= eventKey;
            if (eventRemoved)
            {
                explicitInitialStateIsPresent = false;
            }
        }

        // Retry deferred events once; if still deferred, throw a descriptive exception.
        foreach (var @event in new List<KernelAppendedEvent>(deferredEvents))
        {
            var changeset = new Changeset<KernelAppendedEvent, ExpandoObject>(_objectComparer, @event, state);
            var keyResolver = engineProjection.GetKeyResolverFor(@event.Context.EventType);
            var keyResult = await keyResolver(inMemoryEventSequenceStorage, inMemorySink, @event);

            if (keyResult is KernelProjectionEngine::DeferredKey)
            {
                throw new InvalidOperationException(
                    $"Could not resolve parent key for event '{@event.Context.EventType.Id}' (EventSourceId '{@event.Context.EventSourceId}'). " +
                    "Ensure the parent event is included in the scenario and appended before the child event.");
            }

            var key = (keyResult as KernelProjectionEngine::ResolvedKey)!.Key;
            rootKey ??= key;
            var context = new KernelProjectionEngine::ProjectionEventContext(
                key,
                @event,
                changeset,
                engineProjection.GetOperationTypeFor(@event.Context.EventType),
                false);

            HandleEventFor(engineProjection, context);

            // Apply to the sink before mutating the threaded state — the sink clones the changeset's
            // InitialState (the same object as `state`), so mutating it first would double an additive change.
            if (changeset.HasChanges)
            {
                await inMemorySink.ApplyChanges(key, changeset, @event.Context.SequenceNumber);
            }

            if (changeset.Changes.Any(change => change is Removed))
            {
                state = new ExpandoObject();
                explicitInitialStateIsPresent = false;
            }
            else if (changeset.HasChanges)
            {
                state = ApplyActualChanges(key, changeset.Changes, state);
            }
        }

        // Read every materialized instance per-key from the sink. Unlike the single threaded `state`
        // (which blends events across sources into one object), the sink keeps one document per resolved
        // root key — so a multi-source projection (e.g. a join whose join-source event was seeded first)
        // can be asserted against the intended instance via InstanceForEventSourceId.
        var instances = BuildInstancesFromSink<TReadModel>(inMemorySink);

        // An explicit initial state remains materialized through empty changesets until an actual root removal.
        // Otherwise presence comes from the same per-key sink state that backs Instances: deferred, joined, or
        // unsubscribed no-ops cannot create an id-only model, while a later constructive event recreates it.
        var hasPrimary = explicitInitialStateIsPresent ||
            (rootKey is not null && await inMemorySink.FindOrDefault(rootKey) is not null);
        if (!hasPrimary)
        {
            return (null, instances);
        }

        InjectIdentifierFromKey<TReadModel>(state, rootKey);

        // Serialize with the same options used to deserialize so concept values — both scalar and the
        // elements of a concept collection (e.g. IReadOnlyList<MyConcept>) — are written in their
        // unwrapped primitive form. Without these options a concept CLR object serializes as an object
        // ({ "value": ... }), which the concept-aware deserializer then rejects when it expects the
        // underlying primitive.
        var json = JsonSerializer.Serialize(state, Globals.JsonSerializerOptions);
        var primary = JsonSerializer.Deserialize<TReadModel>(json, _readModelSerializerOptions);
        return (primary, instances);
    }

    /// <summary>
    /// Deserializes every per-key document held by the in-memory sink into a <typeparamref name="TReadModel"/>,
    /// keyed by its event source id — the faithful per-instance view the single threaded state cannot provide.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type being projected.</typeparam>
    /// <param name="sink">The in-memory sink populated during processing.</param>
    /// <returns>A dictionary of every materialized instance keyed by its <see cref="EventSourceId"/>.</returns>
    static Dictionary<EventSourceId, TReadModel> BuildInstancesFromSink<TReadModel>(InMemorySink sink)
        where TReadModel : class
    {
        var instances = new Dictionary<EventSourceId, TReadModel>();
        foreach (var (keyValue, document) in sink.Collection)
        {
            if (keyValue is null)
            {
                continue;
            }

            var json = JsonSerializer.Serialize(document, Globals.JsonSerializerOptions);
            var instance = JsonSerializer.Deserialize<TReadModel>(json, _readModelSerializerOptions);
            if (instance is null)
            {
                continue;
            }

            instances[new EventSourceId(keyValue.ToString()!)] = instance;
        }

        return instances;
    }

    /// <summary>
    /// Mirrors MongoDB's `_id` → identifier property mapping for the in-memory test harness.
    /// Finds the read model's identifier property (preferring <c>[Key]</c>, then <c>[Subject]</c>,
    /// then a property named "Id" by convention) and writes the resolved projection key value into
    /// the state under that property's camel-cased name — unless an event mapping has already
    /// populated that property.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type being projected.</typeparam>
    /// <param name="state">The projection state ExpandoObject to inject into.</param>
    /// <param name="key">The resolved projection key, or <see langword="null"/> if no event resolved one.</param>
    static void InjectIdentifierFromKey<TReadModel>(ExpandoObject state, KernelKey? key)
        where TReadModel : class
    {
        if (key is null || key.Value is null) return;

        var identifierProperty = IdentifierProperty.Find(typeof(TReadModel));
        if (identifierProperty is null) return;

        var propertyName = AcronymFriendlyJsonCamelCaseNamingPolicy.Instance.ConvertName(identifierProperty.Name);
        var stateDict = (IDictionary<string, object?>)state;

        // Don't overwrite an explicit projection mapping (e.g. [SetFrom<T>] or [SetFromContext<T>]
        // that wrote to the identifier property). The MongoDB analogue is: if the projection
        // updates `_id`, that update wins; we only fill in `_id` when no update touched it.
        if (stateDict.TryGetValue(propertyName, out var existing) && existing is not null)
        {
            return;
        }

        stateDict[propertyName] = key.Value;
    }

    /// <summary>
    /// Builds the initial state the projection starts from, seeding every array-typed schema property with an
    /// empty collection - except the ones the projection's own children collections live under.
    /// </summary>
    /// <param name="schema">The read model <see cref="JsonSchema"/>.</param>
    /// <param name="projection">The root <see cref="KernelProjectionEngine::IProjection"/> being applied.</param>
    /// <returns>The initial state.</returns>
    /// <remarks>
    /// The seeding matches the live kernel's <c>ProjectionFactory.CreateInitialState</c>. The exclusion is the
    /// step this harness used to be missing: the live pipeline diffs the initial state into the changeset with
    /// every children-collection path removed, so an untouched child collection is never written and reads back
    /// from the store as an absent field. Seeding it here regardless answered "what is an empty child collection"
    /// with <c>[]</c> - the one answer the running system never gives - at exactly the point the production
    /// pipeline makes a deliberate exception, and so put the question out of reach of every spec at every tier.
    /// <para>
    /// The paths come from the kernel's own <see cref="KernelProjectionEngine::ProjectionExtensions.GetChildrenPropertyPaths"/>
    /// rather than being re-derived here, so a future exclusion is inherited instead of needing to be noticed.
    /// </para>
    /// </remarks>
    static ExpandoObject CreateInitialStateFromSchema(JsonSchema schema, KernelProjectionEngine::IProjection projection)
    {
        var initialState = new ExpandoObject();
        var dict = (IDictionary<string, object?>)initialState;
        var childrenProperties = KernelProjectionEngine::ProjectionExtensions.GetChildrenPropertyPaths(projection)
            .Where(_ => !_.IsRoot)
            .Select(_ => _.Segments.First().Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var property in schema.Properties.Values.Where(p => p.IsArray && !childrenProperties.Contains(p.Name)))
        {
            dict[property.Name] = new List<object>();
        }

        return initialState;
    }

    static KernelProjectionEngine::ProjectionFactory CreateProjectionFactory(Storage.IStorage storage) =>
        new(
            _readModelPropertyExpressionResolvers,
            _eventValueProviderExpressionResolvers,
            _keyExpressionResolvers,
            _expandoObjectConverter,
            _keyResolvers,
            storage,
            FrameworkNullLoggerFactory.Instance.CreateLogger<KernelProjectionEngine::ProjectionFactory>());

    static List<KernelEventTypes::EventTypeSchema> BuildEventTypeSchemas(
        IEnumerable<(EventSourceId EventSourceId, object Event)> events,
        IEventTypes eventTypes,
        IJsonSchemaGenerator jsonSchemaGenerator)
    {
        var seenTypes = new HashSet<Type>();
        var schemas = new List<KernelEventTypes::EventTypeSchema>();

        foreach (var (_, eventInstance) in events)
        {
            var type = eventInstance.GetType();
            if (!seenTypes.Add(type))
            {
                continue;
            }

            var clientEventType = eventTypes.GetEventTypeFor(type);
            var kernelEventType = ToKernelEventType(clientEventType);
            var eventSchema = jsonSchemaGenerator.Generate(type);
            schemas.Add(new KernelEventTypes::EventTypeSchema(
                kernelEventType,
                KernelConceptsNs::Events.EventTypeOwner.Client,
                KernelConceptsNs::Events.EventTypeSource.Code,
                eventSchema));
        }

        return schemas;
    }

    static async Task<(ExpandoObject State, KernelKey? Key, bool Removed)> ProcessSingleEvent(
        KernelProjectionEngine::IProjection projection,
        KernelConceptsNs::Projections.Definitions.ProjectionDefinition kernelProjectionDefinition,
        InMemoryEventSequenceStorage eventSequenceStorage,
        InMemorySink sink,
        KernelAppendedEvent @event,
        ExpandoObject state,
        Queue<KernelAppendedEvent> deferredEvents,
        bool strictEventSubscription)
    {
        // The production projection engine filters the event stream to the types the projection
        // subscribes to, so an event a projection does not handle never reaches it. A seeded stream
        // may legitimately carry such unrelated events (e.g. an audit/marker event); skip them here
        // rather than letting GetKeyResolverFor throw MissingKeyResolverForEventType. HasKeyResolverFor
        // is the exact predicate for "would GetKeyResolverFor succeed" (same resolver map), and the
        // root projection's map includes child ([ChildrenFrom]) event types, so this is a faithful
        // no-op filter for nested/join scenarios too. In strict mode the same condition is surfaced as a
        // loud error instead, so a spec that opted in can catch the genuine mistake of seeding the wrong event.
        if (!projection.HasKeyResolverFor(@event.Context.EventType))
        {
            if (strictEventSubscription)
            {
                throw new UnsubscribedEventSeeded(@event.Context.EventType.Id.ToString());
            }

            return (state, null, false);
        }

        var changeset = new Changeset<KernelAppendedEvent, ExpandoObject>(_objectComparer, @event, state);

        KernelProjectionEngine::KeyResolverResult keyResult;

        // A root-level [Join] source enriches an existing root document; resolve it ourselves so the in-memory
        // sink doesn't materialize a phantom keyed by the join source's own id (see JoinKeyResolver). A null
        // root means the row doesn't exist yet — skip; ResolveJoin backfills when the FROM event arrives.
        var (isRootJoinSource, resolvedRootKey) = await JoinKeyResolver.TryResolveRootJoinKey(projection, kernelProjectionDefinition, sink, @event);
        if (isRootJoinSource)
        {
            if (resolvedRootKey is null)
            {
                return (state, null, false);
            }

            keyResult = KernelProjectionEngine::KeyResolverResult.Resolved(resolvedRootKey);
        }
        else
        {
            try
            {
                keyResult = await projection.GetKeyResolverFor(@event.Context.EventType)(eventSequenceStorage, sink, @event);
            }
            catch (FormatException) when (projection.GetOperationTypeFor(@event.Context.EventType).HasFlag(KernelProjectionEngine::ProjectionOperationType.Join))
            {
                // Defensive fallback for a nested/child [Join] whose join key can't be force-converted to the
                // identifier type: skip rather than crash the scenario (root joins are handled above).
                return (state, null, false);
            }
        }

        if (keyResult is KernelProjectionEngine::DeferredKey)
        {
            deferredEvents.Enqueue(@event);
            return (state, null, false);
        }

        var key = (keyResult as KernelProjectionEngine::ResolvedKey)!.Key;

        // A child-level [Join] source resolves onto a child of an existing root; re-anchor onto that root
        // (keeping the array indexers) so the child is updated in place rather than duplicated into a phantom
        // (see JoinKeyResolver). A null root means the child doesn't exist yet — skip; ResolveJoin backfills it.
        var (isChildJoinSource, childJoinRootKey) = await JoinKeyResolver.TryResolveChildJoinRootKey(projection, sink, @event, key);
        if (isChildJoinSource)
        {
            if (childJoinRootKey is null)
            {
                return (state, null, false);
            }

            key = key with { Value = childJoinRootKey.Value };
        }

        var context = new KernelProjectionEngine::ProjectionEventContext(
            key,
            @event,
            changeset,
            projection.GetOperationTypeFor(@event.Context.EventType),
            false);

        HandleEventFor(projection, context);

        // A root-level removal yields a Removed change, which the real sink applies by deleting the
        // document. Mirror that by resetting the threaded state so any subsequent re-create starts clean.
        var removed = changeset.Changes.Any(change => change is Removed);

        // Apply to the sink BEFORE mutating the threaded state. The sink clones the changeset's InitialState —
        // which is the SAME object as the threaded `state` — so mutating the threaded state first would let the
        // sink re-apply an additive change (e.g. a child add) on top of an already-advanced base, duplicating it.
        if (changeset.HasChanges)
        {
            await sink.ApplyChanges(key, changeset, @event.Context.SequenceNumber);
        }

        var updatedState = removed ? new ExpandoObject() : ApplyActualChanges(key, changeset.Changes, state);
        return (updatedState, key, removed);
    }

    static KernelReadModels::ReadModelDefinition BuildKernelReadModelDefinition(Type readModelType, JsonSchema schema)
    {
        var identifier = (KernelReadModels::ReadModelIdentifier)readModelType.FullName!;
        var containerName = (KernelReadModels::ReadModelContainerName)readModelType.Name;
        var displayName = (KernelReadModels::ReadModelDisplayName)readModelType.Name;
        var sink = new KernelSinks::SinkDefinition(
            KernelSinks::SinkConfigurationId.None,
            KernelSinks::WellKnownSinkTypes.InMemory);

        return new KernelReadModels::ReadModelDefinition(
            identifier,
            containerName,
            displayName,
            KernelReadModels::ReadModelOwner.Client,
            KernelReadModels::ReadModelSource.User,
            KernelReadModels::ReadModelObserverType.Projection,
            KernelReadModels::ReadModelObserverIdentifier.Unspecified,
            sink,
            new Dictionary<KernelReadModels::ReadModelGeneration, JsonSchema>
            {
                { KernelReadModels::ReadModelGeneration.First, schema }
            },
            []);
    }

    static KernelConceptsNs::Events.EventType ToKernelEventType(EventType clientEventType) =>
        new(
            new KernelConceptsNs::Events.EventTypeId(clientEventType.Id.Value),
            new KernelConceptsNs::Events.EventTypeGeneration(clientEventType.Generation.Value));

    static void HandleEventFor(KernelProjectionEngine::IProjection projection, KernelProjectionEngine::ProjectionEventContext context)
    {
        if (projection.Accepts(context.Event.Context.EventType))
        {
            projection.OnNext(context);
        }

        foreach (var child in projection.ChildProjections)
        {
            HandleEventFor(child, context);
        }
    }

    static ExpandoObject ApplyActualChanges(KernelKey key, IEnumerable<Change> changes, ExpandoObject state)
    {
        foreach (var change in changes)
        {
            switch (change)
            {
                case PropertiesChanged<ExpandoObject>:
                    ApplyPropertyDifferences((change as PropertiesChanged<ExpandoObject>)!.Differences, state);
                    break;

                case ChildAdded childAdded:
                    InjectChildIdentifierFromKey(childAdded);
                    var items = EnsureChildCollection(state, childAdded);
                    items.Add(childAdded.Child);
                    break;

                case ChildRemoved childRemoved:
                    ApplyChildRemoved(state, childRemoved);
                    break;

                case NestedCleared nestedCleared:
                    ((IDictionary<string, object?>)state)[nestedCleared.NestedProperty.LastSegment.Value] = null;
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

    /// <summary>
    /// Removes a child from its collection in the projection state, mirroring how the production sinks
    /// apply a <see cref="ChildRemoved"/> change. Walks the children property path and removes from every
    /// matching leaf collection the item whose identifier property equals the removed key.
    /// </summary>
    /// <param name="state">The projection state to mutate.</param>
    /// <param name="childRemoved">The <see cref="ChildRemoved"/> change to apply.</param>
    static void ApplyChildRemoved(ExpandoObject state, ChildRemoved childRemoved)
    {
        var segments = childRemoved.ChildrenProperty.Segments.Select(s => s.Value).ToArray();
        RemoveFromCollection(state, segments, 0, childRemoved.IdentifiedByProperty.Path, childRemoved.Key);
    }

    static void RemoveFromCollection(IDictionary<string, object?> current, string[] segments, int index, string identifiedByProperty, object key)
    {
        if (index >= segments.Length || !current.TryGetValue(segments[index], out var value) || value is not System.Collections.IEnumerable enumerable)
        {
            return;
        }

        var items = enumerable.Cast<object>().ToList();

        if (index == segments.Length - 1)
        {
            items.RemoveAll(item =>
                item is ExpandoObject expando &&
                ((IDictionary<string, object?>)expando).TryGetValue(identifiedByProperty, out var idValue) &&
                Equals(NormalizeStateValue(idValue), NormalizeStateValue(key)));
            current[segments[index]] = items;
            return;
        }

        // Intermediate collection — recurse into each element to reach the leaf collection.
        foreach (var item in items.OfType<ExpandoObject>())
        {
            RemoveFromCollection(item, segments, index + 1, identifiedByProperty, key);
        }
    }

    static void ApplyPropertyDifferences(IEnumerable<PropertyDifference> differences, ExpandoObject state)
    {
        foreach (var difference in differences)
        {
            difference.PropertyPath.SetValue(state, NormalizeStateValue(difference.Changed)!, NormalizeArrayIndexers(difference.ArrayIndexers));
        }
    }

    static List<object> EnsureChildCollection(ExpandoObject state, ChildAdded childAdded)
    {
        var normalizedIndexers = NormalizeArrayIndexers(childAdded.ArrayIndexers).All.ToArray();
        var current = (IDictionary<string, object?>)state;

        foreach (var indexer in normalizedIndexers.Take(Math.Max(0, normalizedIndexers.Length - 1)))
        {
            var collectionName = indexer.ArrayProperty.LastSegment.Value;
            var collection = current.TryGetValue(collectionName, out var existingCollection) && existingCollection is System.Collections.IEnumerable existingEnumerable
                ? existingEnumerable.Cast<object>().ToList()
                : [];

            var element = !indexer.IdentifierProperty.IsSet &&
                indexer.Identifier is int index &&
                collection.Count > index
                    ? collection[index] as ExpandoObject
                    : collection
                        .OfType<ExpandoObject>()
                        .SingleOrDefault(item =>
                        {
                            var itemDictionary = (IDictionary<string, object?>)item;
                            return itemDictionary.TryGetValue(indexer.IdentifierProperty.Path, out var value) &&
                                   Equals(NormalizeStateValue(value), NormalizeStateValue(indexer.Identifier));
                        });

            if (element is null)
            {
                element = new ExpandoObject();
                if (indexer.IdentifierProperty.IsSet)
                {
                    ((IDictionary<string, object?>)element)[indexer.IdentifierProperty.Path] = NormalizeStateValue(indexer.Identifier);
                }

                collection.Add(element);
                current[collectionName] = collection;
            }

            current = element;
        }

        var targetCollectionName = childAdded.ChildrenProperty.LastSegment.Value;
        if (current.TryGetValue(targetCollectionName, out var targetCollection) &&
            targetCollection is System.Collections.IEnumerable targetEnumerable)
        {
            var items = targetEnumerable.Cast<object>().ToList();
            current[targetCollectionName] = items;
            return items;
        }

        var newItems = new List<object>();
        current[targetCollectionName] = newItems;
        return newItems;
    }

    static ArrayIndexers NormalizeArrayIndexers(ArrayIndexers arrayIndexers) =>
        arrayIndexers.IsEmpty
            ? ArrayIndexers.NoIndexers
            : new ArrayIndexers(arrayIndexers.All.Select(indexer => indexer with { Identifier = NormalizeStateValue(indexer.Identifier)! }));

    static object? NormalizeStateValue(object? value)
    {
        if (value is null)
        {
            return null;
        }

        var type = value.GetType();
        var isSpecified = type.GetProperty("IsSpecified");
        var wrappedValue = type.GetProperty("Value");

        if (isSpecified?.PropertyType == typeof(bool) && wrappedValue is not null)
        {
            return wrappedValue.GetValue(value);
        }

        return value;
    }

    static void InjectChildIdentifierFromKey(ChildAdded childAdded)
    {
        if (childAdded.Child is not ExpandoObject childState ||
            !childAdded.IdentifiedByProperty.IsSet ||
            childAdded.IdentifiedByProperty.IsRoot)
        {
            return;
        }

        var propertyName = childAdded.IdentifiedByProperty.LastSegment.Value;
        var childStateDict = (IDictionary<string, object?>)childState;

        childStateDict[propertyName] = NormalizeStateValue(childAdded.Key);
    }
}
