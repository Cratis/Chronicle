// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Testing.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Folds scenario events using the same projection and reducer processors for scenarios and observations.
/// </summary>
internal static class ReadModelFolder
{
    /// <summary>
    /// Processes a single scenario's input through its selected handler.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="events">The routed events.</param>
    /// <param name="eventStore">The owning store.</param>
    /// <param name="eventTypes">The event type registry.</param>
    /// <param name="schemaGenerator">The schema generator.</param>
    /// <param name="activator">The artifact activator.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="namingPolicy">The naming policy.</param>
    /// <param name="reducerType">The selected reducer, if any.</param>
    /// <param name="projectionDefinition">The selected projection, if any.</param>
    /// <param name="initialState">Optional initial state.</param>
    /// <param name="strictEventSubscription">Whether unsubscribed input is refused.</param>
    /// <param name="eventContexts">Original contexts when folding the store's event log.</param>
    /// <returns>The primary instance and every keyed instance.</returns>
    /// <exception cref="NoReadModelHandlerFound">No handler exists for the read model.</exception>
    public static async Task<(TReadModel? Primary, IReadOnlyDictionary<EventSourceId, TReadModel> Instances)> Process<TReadModel>(
        IEnumerable<ReadModelEvent> events,
        EventStoreForTesting eventStore,
        IEventTypes eventTypes,
        IJsonSchemaGenerator schemaGenerator,
        IClientArtifactsActivator activator,
        IServiceProvider serviceProvider,
        INamingPolicy namingPolicy,
        Type? reducerType,
        Contracts.Projections.ProjectionDefinition? projectionDefinition,
        TReadModel? initialState = null,
        bool strictEventSubscription = false,
        IReadOnlyList<EventContext>? eventContexts = null)
        where TReadModel : class
    {
        var eventsList = events.ToList();
        if (reducerType is not null)
        {
            var (reduced, distinctEventSourceIds) = await ReducerReadModelProcessor.Process(
                reducerType,
                eventsList.Select(e => new EventForEventSourceId(e.EventSourceId, e.Event, Causation.Unknown())
                {
                    EventSourceType = e.EventSourceType,
                    EventStreamType = e.EventStreamType,
                    EventStreamId = e.EventStreamId
                }),
                eventStore,
                eventTypes,
                activator,
                serviceProvider,
                namingPolicy,
                initialState,
                eventContexts);

            var reducerInstances = new Dictionary<EventSourceId, TReadModel>();
            if (reduced is not null && distinctEventSourceIds.Count == 1)
            {
                reducerInstances[distinctEventSourceIds[0]] = reduced;
            }

            return (reduced, reducerInstances);
        }

        if (projectionDefinition is not null)
        {
            return await ProjectionReadModelProcessor.Process(
                projectionDefinition,
                eventsList.Select(e => (e.EventSourceId, e.Event)),
                eventStore,
                eventTypes,
                schemaGenerator,
                initialState,
                strictEventSubscription,
                eventContexts,
                seededEvents: eventsList);
        }

        throw new NoReadModelHandlerFound(typeof(TReadModel));
    }

    /// <summary>
    /// Rebuilds a collection, reducing each event source separately.
    /// </summary>
    /// <typeparam name="TReadModel">The read model type.</typeparam>
    /// <param name="store">The store supplying discovered artifacts.</param>
    /// <param name="events">The ordered log snapshot.</param>
    /// <param name="useOriginalContexts">Whether the events belong to this store's log.</param>
    /// <returns>The rebuilt instances indexed by their materialized keys.</returns>
    public static async Task<IReadOnlyDictionary<EventSourceId, TReadModel>> Fold<TReadModel>(EventStoreForTesting store, IEnumerable<AppendedEvent> events, bool useOriginalContexts)
        where TReadModel : class
    {
        var namingPolicy = new CamelCaseNamingPolicy();
        using var loggerFactory = new NullLoggerFactory();
        var activator = new ClientArtifactsActivator(store.ServiceProvider, loggerFactory);
        var hasExplicitProjection = store.ClientOptions.ExplicitArtifacts.Projections.Any(projection => projection.ReadModelType == typeof(TReadModel));
        var reducerType = !hasExplicitProjection && store.Reducers.HasFor<TReadModel>()
            ? store.Reducers.GetHandlerForReadModelType(typeof(TReadModel)).ReducerType
            : null;
        var definition = ((Projections.Projections)store.Projections).Definitions.FirstOrDefault(definition =>
            definition.ReadModel == typeof(TReadModel).GetReadModelIdentifier().Value);
        var appended = events.ToArray();
        var inputs = appended.Select(ToInput).ToArray();
        if (reducerType is not null)
        {
            var instances = new Dictionary<EventSourceId, TReadModel>();
            foreach (var group in appended.GroupBy(e => e.Context.EventSourceId))
            {
                var (primary, _) = await Process<TReadModel>(group.Select(ToInput), store, store.EventTypes, store.JsonSchemaGenerator, activator, store.ServiceProvider, namingPolicy, reducerType, null, eventContexts: useOriginalContexts ? group.Select(e => e.Context).ToArray() : null);
                if (primary is not null) instances[group.Key] = primary;
            }

            return instances;
        }

        var (_, projected) = await Process<TReadModel>(inputs, store, store.EventTypes, store.JsonSchemaGenerator, activator, store.ServiceProvider, namingPolicy, null, definition, eventContexts: useOriginalContexts ? appended.Select(e => e.Context).ToArray() : null);
        return projected;
    }

    static ReadModelEvent ToInput(AppendedEvent e) => new(e.Context.EventSourceId, e.Content, e.Context.EventSourceType, e.Context.EventStreamType, e.Context.EventStreamId);
}
