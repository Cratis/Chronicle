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
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sinks;
using Cratis.Chronicle.Testing.EventSequences;
using Cratis.Json;
using Microsoft.Extensions.Logging;
using FrameworkNullLoggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory;
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
    /// <returns>The projected read model, or <see langword="null"/> if the projection did not apply any changes.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a deferred key cannot be resolved after retrying all events.</exception>
    public static async Task<TReadModel?> Process<TReadModel>(
        Contracts.Projections.ProjectionDefinition projectionDefinition,
        IEnumerable<(EventSourceId EventSourceId, object Event)> events,
        IEventTypes eventTypes,
        IJsonSchemaGenerator jsonSchemaGenerator,
        TReadModel? initialState = null)
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
                    SequenceNumber = (KernelConceptsNs::Events.EventSequenceNumber)(uint)index
                };
                return new KernelAppendedEvent(context, content);
            })
            .ToArray();

        // Populate in-memory event sequence storage with all events so key resolvers
        // (e.g. FromParentHierarchy for ChildrenFrom projections) can look up parent events
        // across separate event source streams.
        var eventSequenceId = KernelConceptsNs::EventSequences.EventSequenceId.Log;
        var inMemoryEventSequenceStorage = new InMemoryEventSequenceStorage(eventSequenceId);
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
                });
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
            ? initialState.AsExpandoObject(false)
            : new ExpandoObject();

        // Process events, retrying any that return DeferredKey once all other events have been processed.
        var deferredEvents = new Queue<KernelAppendedEvent>();
        foreach (var @event in appendedEvents)
        {
            state = await ProcessSingleEvent(engineProjection, inMemoryEventSequenceStorage, @event, state, deferredEvents);
        }

        // Retry deferred events once; if still deferred, throw a descriptive exception.
        foreach (var @event in new List<KernelAppendedEvent>(deferredEvents))
        {
            var changeset = new Changeset<KernelAppendedEvent, ExpandoObject>(_objectComparer, @event, state);
            var keyResolver = engineProjection.GetKeyResolverFor(@event.Context.EventType);
            var keyResult = await keyResolver(inMemoryEventSequenceStorage, NullSink.Instance, @event);

            if (keyResult is KernelProjectionEngine::DeferredKey)
            {
                throw new InvalidOperationException(
                    $"Could not resolve parent key for event '{@event.Context.EventType.Id}' (EventSourceId '{@event.Context.EventSourceId}'). " +
                    "Ensure the parent event is included in the scenario and appended before the child event.");
            }

            var key = (keyResult as KernelProjectionEngine::ResolvedKey)!.Key;
            var context = new KernelProjectionEngine::ProjectionEventContext(
                key,
                @event,
                changeset,
                engineProjection.GetOperationTypeFor(@event.Context.EventType),
                false);

            HandleEventFor(engineProjection, context);
            state = ApplyActualChanges(key, changeset.Changes, state);
        }

        var json = JsonSerializer.Serialize(state);
        return JsonSerializer.Deserialize<TReadModel>(json, Globals.JsonSerializerOptions);
    }

    static KernelProjectionEngine::ProjectionFactory CreateProjectionFactory(global::Cratis.Chronicle.Storage.IStorage storage) =>
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

    static async Task<ExpandoObject> ProcessSingleEvent(
        KernelProjectionEngine::IProjection projection,
        InMemoryEventSequenceStorage eventSequenceStorage,
        KernelAppendedEvent @event,
        ExpandoObject state,
        Queue<KernelAppendedEvent> deferredEvents)
    {
        var changeset = new Changeset<KernelAppendedEvent, ExpandoObject>(_objectComparer, @event, state);
        var keyResult = await projection.GetKeyResolverFor(@event.Context.EventType)(eventSequenceStorage, NullSink.Instance, @event);

        if (keyResult is KernelProjectionEngine::DeferredKey)
        {
            deferredEvents.Enqueue(@event);
            return state;
        }

        var key = (keyResult as KernelProjectionEngine::ResolvedKey)!.Key;
        var context = new KernelProjectionEngine::ProjectionEventContext(
            key,
            @event,
            changeset,
            projection.GetOperationTypeFor(@event.Context.EventType),
            false);

        HandleEventFor(projection, context);
        return ApplyActualChanges(key, changeset.Changes, state);
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
}
