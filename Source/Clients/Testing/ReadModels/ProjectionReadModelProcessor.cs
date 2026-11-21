// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelConcepts;
extern alias KernelCore;
extern alias KernelGrpc;

using System.Dynamic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Json;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;
using Cratis.Chronicle.Storage.InMemory.Sinks;
using Cratis.Json;
using Microsoft.Extensions.Logging;
using FrameworkNullLoggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory;
using KernelAppendedEvent = KernelConcepts::Cratis.Chronicle.Concepts.Events.AppendedEvent;
using KernelConceptsNs = KernelConcepts::Cratis.Chronicle.Concepts;
using KernelKey = KernelConcepts::Cratis.Chronicle.Concepts.Keys.Key;
using KernelProjectionEngine = KernelCore::Cratis.Chronicle.Projections.Engine;
using KernelReadModels = KernelConcepts::Cratis.Chronicle.Concepts.ReadModels;
using KernelSinks = KernelConcepts::Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Processes events through a projection engine to produce a read model instance for testing.
/// </summary>
internal static partial class ProjectionReadModelProcessor
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
    /// Gets a value indicating whether this processor runs Chronicle's compliance and security stack.
    /// </summary>
    /// <remarks>
    /// The live projection pipeline runs <c language="csharp">EncryptChangeset</c> before the sink and releases what it reads
    /// back; the pipeline below runs neither, so a <c language="csharp">[PII]</c> or <c language="csharp">[Encrypted]</c> member is
    /// projected and read as plaintext. Wire compliance and security in and flip this, and <see cref="SubstitutedLayers"/>
    /// stops reporting <see cref="ReadModelSubstitutedLayer.Compliance"/>.
    /// </remarks>
    public static bool AppliesCompliance => false;

    /// <summary>
    /// Processes the given events through the projection for <typeparamref name="TReadModel"/> and returns the resulting read model.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model produced by the projection.</typeparam>
    /// <param name="projectionDefinition">The client-side <see cref="Contracts.Projections.ProjectionDefinition"/>.</param>
    /// <param name="events">The events with their associated <see cref="EventSourceId"/> to process.</param>
    /// <param name="context">The storage, artifacts, services and original event contexts for the fold.</param>
    /// <param name="initialState">Optional initial read model state.</param>
    /// <param name="strictEventSubscription">
    /// When <see langword="false"/> (the default), a seeded event the projection does not subscribe to is
    /// silently skipped, matching the production engine. When <see langword="true"/>, such an event raises
    /// <see cref="UnsubscribedEventSeeded"/> instead.
    /// </param>
    /// <param name="seededEvents">Optional routed synthetic input, used when original contexts are not supplied.</param>
    /// <param name="seededInstances">Optional per-key persisted starting state, including each instance's identifier.</param>
    /// <returns>
    /// A tuple of the primary projected read model (the instance for the first key resolved, exposed as
    /// <c language="csharp">Instance</c>, or <see langword="null"/> if the projection did not apply any changes) and a
    /// dictionary of every materialized instance keyed by its event source id (read per-key from the sink,
    /// so a multi-source projection such as a join can be asserted against the intended instance
    /// deterministically).
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown when a deferred key cannot be resolved after retrying all events.</exception>
    /// <exception cref="UnsubscribedEventSeeded">Thrown when strict mode is enabled and a seeded event is not subscribed to.</exception>
    public static async Task<(TReadModel? Primary, IReadOnlyDictionary<EventSourceId, TReadModel> Instances)> Process<TReadModel>(
        Contracts.Projections.ProjectionDefinition projectionDefinition,
        IEnumerable<(EventSourceId EventSourceId, object Event)> events,
        ReadModelFoldContext context,
        TReadModel? initialState = null,
        bool strictEventSubscription = false,
        IReadOnlyList<ReadModelEvent>? seededEvents = null,
        IEnumerable<(object Key, TReadModel Instance)>? seededInstances = null)
        where TReadModel : class
    {
        var readModelType = context.ReadModelType;
        var eventStore = context.EventStore;
        var eventContexts = context.EventContexts;
        var schema = context.SchemaGenerator.GenerateForReadModel(readModelType);

        var kernelReadModelDefinition = BuildKernelReadModelDefinition(readModelType, schema);
        var kernelProjectionDefinition = ToKernelProjectionDefinition(projectionDefinition, context);

        var eventsList = events.ToList();

        // Keep synthetic positions and observer-free folding; prepare migrations through the kernel component.
        var (appendedEvents, inMemoryEventSequenceStorage) = await ReadModelEvents.Prepare(
            eventStore,
            eventsList,
            eventContexts,
            persist: eventContexts is null || context.InputSequenceId is not null,
            seededEvents: seededEvents,
            inputSequenceId: context.InputSequenceId);
        var eventTypeSchemas = await eventStore.TestingStore.Store.EventTypes.GetFor(context.EventTypes.All.Select(ToKernelEventType));
        var projectionFactory = CreateProjectionFactory(eventStore.TestingStore.Storage);

        var engineProjection = await projectionFactory.Create(
            new(eventStore.Name.Value),
            new(eventStore.Namespace.Value),
            kernelProjectionDefinition,
            kernelReadModelDefinition,
            eventTypeSchemas);

        // The state every read model instance starts from, produced fresh per instance. The live pipeline
        // clones the projection's initial model state for each key it has not seen (SetInitialState), so a
        // second event source starts from an empty model rather than inheriting the first one's values —
        // which is what a shared, threaded state silently did, carrying one root's children onto the next.
        ExpandoObject CreateSeedState() => initialState is not null
            ? CreateSeedStateFromInitialState(initialState, schema)
            : CreateInitialStateFromSchema(schema, engineProjection);

        using var inMemorySink = new InMemorySink(kernelReadModelDefinition, _typeFormats);
        var run = new FoldRun
        {
            Projection = engineProjection,
            ProjectionDefinition = kernelProjectionDefinition,
            Storage = inMemoryEventSequenceStorage,
            Sink = inMemorySink,
            ReadModelType = readModelType,
            CreateSeedState = CreateSeedState,
            StrictEventSubscription = strictEventSubscription,
            ExplicitInitialStateIsPresent = initialState is not null
        };

        foreach (var (key, instance) in seededInstances ?? [])
        {
            run.Seed(key, () => CreateSeedStateFromInitialState(instance, schema));
        }

        await run.ProcessEvents(appendedEvents);
        run.ThrowIfEventsAreUnresolved();

        // Read every materialized instance per-key from the sink, which keeps one document per resolved
        // root key — so a multi-source projection (e.g. a join whose join-source event was seeded first)
        // can be asserted against the intended instance via InstanceForEventSourceId.
        var instances = BuildInstancesFromSink<TReadModel>(inMemorySink, readModelType);
        return (await SelectPrimary(run, instances), instances);
    }

    /// <summary>
    /// Creates a sink using the same read model schema and key conversion as projection folding.
    /// </summary>
    /// <param name="readModelType">The read model type.</param>
    /// <param name="schemaGenerator">The schema generator used by the fold.</param>
    /// <returns>A sink whose key conversion matches the materialized instances.</returns>
    internal static InMemorySink CreateSink(Type readModelType, IJsonSchemaGenerator schemaGenerator) =>
        new(BuildKernelReadModelDefinition(readModelType, schemaGenerator.GenerateForReadModel(readModelType)), _typeFormats);

    /// <summary>
    /// Creates a projection factory that uses the same resolvers as projection folding.
    /// </summary>
    /// <param name="storage">The storage the factory reads from.</param>
    /// <returns>The <see cref="KernelProjectionEngine::ProjectionFactory"/>.</returns>
    internal static KernelProjectionEngine::ProjectionFactory CreateProjectionFactory(Storage.IStorage storage) =>
        new(
            _readModelPropertyExpressionResolvers,
            _eventValueProviderExpressionResolvers,
            _keyExpressionResolvers,
            _expandoObjectConverter,
            _keyResolvers,
            storage,
            FrameworkNullLoggerFactory.Instance.CreateLogger<KernelProjectionEngine::ProjectionFactory>());

    /// <summary>
    /// Gets the identifier schema used by native projection folding.
    /// </summary>
    /// <param name="readModelType">The model type.</param>
    /// <param name="schemaGenerator">The fold's schema generator.</param>
    /// <returns>The native schema.</returns>
    internal static JsonSchema GetReadModelSchema(Type readModelType, IJsonSchemaGenerator schemaGenerator) =>
        BuildKernelReadModelDefinition(readModelType, schemaGenerator.GenerateForReadModel(readModelType)).GetSchemaForLatestGeneration();

    static KernelConceptsNs::Projections.Definitions.ProjectionDefinition ToKernelProjectionDefinition(
        Contracts.Projections.ProjectionDefinition projectionDefinition,
        ReadModelFoldContext context) =>
        KernelGrpc::Cratis.Chronicle.Services.Projections.Definitions.ProjectionDefinitionConverters.ToChronicle(
            projectionDefinition,
            KernelConceptsNs::Projections.ProjectionOwner.Client) with
        {
            EventSequenceId = new(context.InputSequenceId?.Value ?? (context.EventContexts is null ? ReadModelEvents.SequenceId.Value : context.EventStore.EventLog.Id.Value))
        };

    static async Task<TReadModel?> SelectPrimary<TReadModel>(FoldRun run, Dictionary<EventSourceId, TReadModel> instances)
        where TReadModel : class
    {
        if (run.HasSeededInstances && instances.Count == 1)
        {
            return instances.Values.Single();
        }

        if (!await run.HasPrimaryInstance())
        {
            return run.HasSeededInstances ? instances.Values.FirstOrDefault() : null;
        }

        // Serialize with the same options used to deserialize so concept values — both scalar and the
        // elements of a concept collection (e.g. IReadOnlyList<MyConcept>) — are written in their
        // unwrapped primitive form. Without these options a concept CLR object serializes as an object
        // ({ "value": ... }), which the concept-aware deserializer then rejects when it expects the
        // underlying primitive.
        var json = JsonSerializer.Serialize(run.GetPrimaryState(), Globals.JsonSerializerOptions);
        return (TReadModel?)JsonSerializer.Deserialize(json, run.ReadModelType, _readModelSerializerOptions);
    }

    /// <summary>
    /// Deserializes every per-key document held by the in-memory sink into a <typeparamref name="TReadModel"/>,
    /// keyed by its event source id.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type being projected.</typeparam>
    /// <param name="sink">The in-memory sink populated during processing.</param>
    /// <param name="readModelType">The runtime read model type.</param>
    /// <returns>A dictionary of every materialized instance keyed by its <see cref="EventSourceId"/>.</returns>
    static Dictionary<EventSourceId, TReadModel> BuildInstancesFromSink<TReadModel>(InMemorySink sink, Type readModelType)
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
            var instance = (TReadModel?)JsonSerializer.Deserialize(json, readModelType, _readModelSerializerOptions);
            if (instance is null)
            {
                continue;
            }

            var key = keyValue is IComparable comparable
                ? EventSourceId<IComparable>.ConvertToString(comparable)
                : keyValue.ToString()!;
            instances[new EventSourceId(key)] = instance;
        }

        return instances;
    }

    /// <summary>
    /// Converts an explicit initial read model state into the state the projection starts from.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type being projected.</typeparam>
    /// <param name="initialState">The initial read model state supplied to the scenario.</param>
    /// <param name="schema">The read model <see cref="JsonSchema"/>.</param>
    /// <returns>A fresh state holding the initial read model's values.</returns>
    /// <remarks>
    /// The conversion is driven by the serialized JSON and the read model schema, the same way seeded event input
    /// is converted, and is the inverse of the serialization the materialized state is read back through. A
    /// reflection walk of the CLR object follows every property, including the parent links inside a
    /// <see cref="JsonNode"/>, and never terminates on a nonempty <see cref="JsonObject"/>. Serializing on every
    /// call also gives each instance its own copy, so no two keys share nested state.
    /// </remarks>
    static ExpandoObject CreateSeedStateFromInitialState<TReadModel>(TReadModel initialState, JsonSchema schema)
        where TReadModel : class
    {
        var stateJson = JsonSerializer.Serialize(initialState, Globals.JsonSerializerOptions);
        var stateObject = JsonNode.Parse(stateJson, new JsonNodeOptions { PropertyNameCaseInsensitive = false })!.AsObject();
        return _expandoObjectConverter.ToExpandoObject(stateObject, schema);
    }

    /// <summary>
    /// Builds the initial state the projection starts from, seeding every array-typed schema property with an
    /// empty collection - except the ones the projection's own children collections live under.
    /// </summary>
    /// <param name="schema">The read model <see cref="JsonSchema"/>.</param>
    /// <param name="projection">The root <see cref="KernelProjectionEngine::IProjection"/> being applied.</param>
    /// <returns>The initial state.</returns>
    /// <remarks>
    /// The seeding matches the live kernel's <c language="csharp">ProjectionFactory.CreateInitialState</c>. The exclusion is the
    /// step this harness used to be missing: the live pipeline diffs the initial state into the changeset with
    /// every children-collection path removed, so an untouched child collection is never written and reads back
    /// from the store as an absent field. Seeding it here regardless answered "what is an empty child collection"
    /// with <c language="csharp">[]</c> - the one answer the running system never gives - at exactly the point the production
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

    /// <summary>
    /// Applies an event whose key is resolved to the state of the instance that key addresses, and to the sink.
    /// </summary>
    /// <param name="projection">The <see cref="KernelProjectionEngine::IProjection"/> being applied.</param>
    /// <param name="eventSequenceStorage">Storage used by child key resolvers to look up related events.</param>
    /// <param name="sink">The <see cref="InMemorySink"/> holding one document per resolved root key.</param>
    /// <param name="event">The event to apply.</param>
    /// <param name="key">The resolved <see cref="KernelKey"/> the event projects onto.</param>
    /// <param name="statesByKey">The state of each instance materialized so far, keyed as the sink keys its documents.</param>
    /// <param name="createSeedState">Produces the state a not-yet-seen instance starts from.</param>
    /// <param name="deferredChildren">Child branches whose keys must be retried after the seeded events.</param>
    /// <param name="joinKey">The join source value, which may differ from the resolved root key.</param>
    /// <returns>True when the event removed the root instance.</returns>
    /// <remarks>
    /// State is held per instance rather than threaded across every seeded event, mirroring the live pipeline's
    /// <c language="csharp">SetInitialState</c>: an event only ever sees the state of the instance its own key addresses. The key
    /// comes from <see cref="InMemorySink.GetKeyValue"/> so that "the same instance" means the same thing here
    /// as it does to the sink — a concept key and its underlying primitive address one document, not two.
    /// </remarks>
    static async Task<bool> ApplyResolvedEvent(
        KernelProjectionEngine::IProjection projection,
        IEventSequenceStorage eventSequenceStorage,
        InMemorySink sink,
        KernelAppendedEvent @event,
        KernelKey key,
        Dictionary<object, ExpandoObject> statesByKey,
        Func<ExpandoObject> createSeedState,
        List<(KernelProjectionEngine::IProjection Child, KernelAppendedEvent Event)> deferredChildren,
        object? joinKey = null)
    {
        var stateKey = sink.GetKeyValue(key);
        var state = GetOrCreateState(projection, key, stateKey, statesByKey, createSeedState);

        var changeset = new Changeset<KernelAppendedEvent, ExpandoObject>(_objectComparer, @event, state);
        var context = new KernelProjectionEngine::ProjectionEventContext(
            key,
            @event,
            changeset,
            projection.GetOperationTypeFor(@event.Context.EventType),
            false,
            joinKey ?? key.Value);

        await HandleEventFor(projection, context, eventSequenceStorage, sink, deferredChildren);

        // A root-level removal yields a Removed change, which the real sink applies by deleting the
        // document. Mirror that by resetting the instance state so any subsequent re-create starts clean.
        var removed = changeset.Changes.Any(change => change is Removed);

        // The live pipeline writes the instance for a root event that creates it even when the event changes nothing
        // (SetInitialState), so an event without properties still brings its read model into existence.
        var createsInstance = IsRootInstanceCreation(projection, @event, context, changeset) &&
            await sink.FindOrDefault(key) is null;

        // Apply to the sink BEFORE mutating the instance state. The sink clones the changeset's InitialState —
        // which is the SAME object as `state` — so mutating the state first would let the sink re-apply an
        // additive change (e.g. a child add) on top of an already-advanced base, duplicating it.
        if (changeset.HasChanges || createsInstance)
        {
            await sink.ApplyChanges(key, changeset, @event.Context.SequenceNumber);
        }

        RemoveChildrenFromAllStates(changeset, statesByKey);

        statesByKey[stateKey] = removed ? new ExpandoObject() : ApplyActualChanges(key, changeset.Changes, state);
        return removed;
    }

    static ExpandoObject GetOrCreateState(
        KernelProjectionEngine::IProjection projection,
        KernelKey key,
        object stateKey,
        Dictionary<object, ExpandoObject> statesByKey,
        Func<ExpandoObject> createSeedState)
    {
        if (!statesByKey.TryGetValue(stateKey, out var state))
        {
            state = createSeedState();
            SetKeyOnSeedState(projection, state, key);
            statesByKey[stateKey] = state;
        }

        return state;
    }

    static bool IsRootInstanceCreation(
        KernelProjectionEngine::IProjection projection,
        KernelAppendedEvent @event,
        KernelProjectionEngine::ProjectionEventContext context,
        Changeset<KernelAppendedEvent, ExpandoObject> changeset) =>
        !projection.HasParent &&
        projection.Accepts(@event.Context.EventType) &&
        context.CreatesInstance &&
        !changeset.HasBeenRemoved();

    static void RemoveChildrenFromAllStates(Changeset<KernelAppendedEvent, ExpandoObject> changeset, Dictionary<object, ExpandoObject> statesByKey)
    {
        foreach (var childRemovedFromAll in changeset.Changes.OfType<ChildRemovedFromAll>())
        {
            var segments = childRemovedFromAll.ChildrenProperty.Segments.Select(segment => segment.Value).ToArray();
            foreach (var parentState in statesByKey.Values)
            {
                RemoveFromCollection(parentState, segments, childRemovedFromAll.IdentifiedByProperty, childRemovedFromAll.Key);
            }
        }
    }

    /// <summary>
    /// Puts the resolved key on a new root instance's state under the read model's key property, as the live
    /// pipeline's SetInitialState step does, so a join keyed on the identifier resolves while the instance is being created.
    /// </summary>
    /// <param name="projection">The <see cref="KernelProjectionEngine::IProjection"/> the instance belongs to.</param>
    /// <param name="state">The state of the new instance.</param>
    /// <param name="key">The resolved <see cref="KernelKey"/> of the instance.</param>
    static void SetKeyOnSeedState(KernelProjectionEngine::IProjection projection, ExpandoObject state, KernelKey key)
    {
        if (projection.HasParent || key.Value is null)
        {
            return;
        }

        var schema = projection.TargetReadModelSchema;
        var keyPropertyName = schema.HasKeyProperty() ? schema.GetKeyProperty().Name : schema.GetLikelyKeyPropertyName();
        if (keyPropertyName is null)
        {
            return;
        }

        var dictionary = (IDictionary<string, object?>)state;
        if (!dictionary.TryGetValue(keyPropertyName, out var existing) || existing is null)
        {
            dictionary[keyPropertyName] = key.Value;
        }
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

    /// <summary>
    /// Hands an event to a projection and then to each of its child projections, mirroring
    /// <c language="csharp">Cratis.Chronicle.Projections.Engine.Pipelines.Steps.HandleEvent.Perform</c>.
    /// </summary>
    /// <param name="projection">The <see cref="KernelProjectionEngine::IProjection"/> to hand the event to.</param>
    /// <param name="context">The <see cref="KernelProjectionEngine::ProjectionEventContext"/> for the event.</param>
    /// <param name="eventSequenceStorage">Storage used by child key resolvers to look up related events.</param>
    /// <param name="sink">Sink used by child key resolvers to look up already projected state.</param>
    /// <param name="deferredChildren">Child branches whose keys must be retried after the seeded events.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// A child owns its own key resolver, which produces the array indexer identifying the child item within its
    /// collection. Reusing the parent's key here leaves the child's collection without an indexer, and the child's
    /// Project() subscription silently no-ops — the parent scalars materialize while the child collection stays
    /// empty. That is only observable when one event type both maps parent fields and feeds a child, because
    /// ProjectionFactory folds a child's resolver into the root's map only for event types the root does not
    /// already own.
    /// </remarks>
    static async Task HandleEventFor(
        KernelProjectionEngine::IProjection projection,
        KernelProjectionEngine::ProjectionEventContext context,
        IEventSequenceStorage eventSequenceStorage,
        InMemorySink sink,
        List<(KernelProjectionEngine::IProjection Child, KernelAppendedEvent Event)> deferredChildren)
    {
        var eventType = context.Event.Context.EventType;
        if (projection.Accepts(eventType))
        {
            projection.OnNext(context);
        }

        foreach (var child in projection.ChildProjections)
        {
            // A projection registers a key resolver for every event type its whole subtree handles, so a missing
            // resolver here means neither this child nor any of its descendants project this event.
            if (!child.HasKeyResolverFor(eventType))
            {
                continue;
            }

            var keyResult = await ResolveChildKey(child, context, eventSequenceStorage, sink);

            // The kernel stores a deferred child for redelivery once its parent exists. The harness
            // retries that branch after processing the seeded events; an unresolvable key is still skipped.
            if (keyResult is KernelProjectionEngine::DeferredKey)
            {
                deferredChildren.Add((child, context.Event));
                continue;
            }

            if (keyResult is KernelProjectionEngine::UnresolvableKey)
            {
                continue;
            }

            var key = (keyResult as KernelProjectionEngine::ResolvedKey)!.Key;
            var operationType = child.GetOperationTypeFor(eventType);
            await HandleEventFor(child, context with { Key = key, OperationType = operationType }, eventSequenceStorage, sink, deferredChildren);
        }
    }

    static async Task<KernelProjectionEngine::KeyResolverResult> ResolveChildKey(
        KernelProjectionEngine::IProjection child,
        KernelProjectionEngine::ProjectionEventContext context,
        IEventSequenceStorage eventSequenceStorage,
        InMemorySink sink)
    {
        var keyResolver = child.GetKeyResolverFor(context.Event.Context.EventType);
        if (!context.TryGetResolvedKey(keyResolver, out var keyResult))
        {
            keyResult = await keyResolver(eventSequenceStorage, sink, context.Event);
            context.MemoizeResolvedKey(keyResolver, keyResult);
        }

        return keyResult;
    }
}
