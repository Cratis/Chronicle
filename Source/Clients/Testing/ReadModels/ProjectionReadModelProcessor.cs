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
internal static class ProjectionReadModelProcessor
{
    static readonly KernelProjectionEngine::IProjectionFactory _projectionFactory;
    static readonly IObjectComparer _objectComparer;

    static ProjectionReadModelProcessor()
    {
        var typeFormats = new TypeFormats();
        var nullLoggerFactory = FrameworkNullLoggerFactory.Instance;

        var eventValueProviderExpressionResolvers = new KernelProjectionEngine::Expressions.EventValues.EventValueProviderExpressionResolvers(
            typeFormats,
            nullLoggerFactory.CreateLogger<KernelProjectionEngine::Expressions.EventValues.EventValueProviderExpressionResolvers>());

        var keyResolvers = new KernelProjectionEngine::KeyResolvers(
            nullLoggerFactory.CreateLogger<KernelProjectionEngine::KeyResolvers>());

        var readModelPropertyExpressionResolvers = new KernelProjectionEngine::Expressions.ReadModelPropertyExpressionResolvers(
            eventValueProviderExpressionResolvers,
            typeFormats,
            nullLoggerFactory.CreateLogger<KernelProjectionEngine::Expressions.ReadModelPropertyExpressionResolvers>());

        var keyExpressionResolvers = new KernelProjectionEngine::Expressions.Keys.KeyExpressionResolvers(
            eventValueProviderExpressionResolvers,
            keyResolvers,
            nullLoggerFactory.CreateLogger<KernelProjectionEngine::Expressions.Keys.KeyExpressionResolvers>());

        var expandoObjectConverter = new ExpandoObjectConverter(typeFormats);

        _projectionFactory = new KernelProjectionEngine::ProjectionFactory(
            readModelPropertyExpressionResolvers,
            eventValueProviderExpressionResolvers,
            keyExpressionResolvers,
            expandoObjectConverter,
            keyResolvers,
            NullKernelStorage.Instance,
            nullLoggerFactory.CreateLogger<KernelProjectionEngine::ProjectionFactory>());

        _objectComparer = new ObjectComparer();
    }

    /// <summary>
    /// Processes the given events through the projection for <typeparamref name="TReadModel"/> and returns the resulting read model.
    /// </summary>
    /// <typeparam name="TReadModel">Type of read model produced by the projection.</typeparam>
    /// <param name="projectionDefinition">The client-side <see cref="Contracts.Projections.ProjectionDefinition"/>.</param>
    /// <param name="events">The events to process.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/> for looking up event type metadata.</param>
    /// <param name="jsonSchemaGenerator"><see cref="IJsonSchemaGenerator"/> for building the read model schema.</param>
    /// <param name="initialState">Optional initial read model state.</param>
    /// <returns>The projected read model, or <c>null</c> if the projection did not apply any changes.</returns>
    public static async Task<TReadModel?> Process<TReadModel>(
        Contracts.Projections.ProjectionDefinition projectionDefinition,
        IEnumerable<object> events,
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

        var engineProjection = await _projectionFactory.Create(
            KernelConceptsNs::EventStoreName.NotSet,
            KernelConceptsNs::EventStoreNamespaceName.NotSet,
            kernelProjectionDefinition,
            kernelReadModelDefinition,
            []);

        var appendedEvents = events
            .Select((eventInstance, index) =>
            {
                var clientEventType = eventTypes.GetEventTypeFor(eventInstance.GetType());
                var kernelEventType = ToKernelEventType(clientEventType);
                var content = eventInstance.AsExpandoObject(true);
                var context = KernelConceptsNs::Events.EventContext.Empty with
                {
                    EventType = kernelEventType,
                    SequenceNumber = (KernelConceptsNs::Events.EventSequenceNumber)(uint)index
                };
                return new KernelAppendedEvent(context, content);
            })
            .ToArray();

        var state = initialState is not null
            ? initialState.AsExpandoObject(false)
            : new ExpandoObject();

        foreach (var @event in appendedEvents)
        {
            var changeset = new Changeset<KernelAppendedEvent, ExpandoObject>(_objectComparer, @event, state);
            var keyResolver = engineProjection.GetKeyResolverFor(@event.Context.EventType);
            var keyResult = await keyResolver(NullEventSequenceStorage.Instance, NullSink.Instance, @event);

            if (keyResult is KernelProjectionEngine::DeferredKey)
            {
                continue;
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
