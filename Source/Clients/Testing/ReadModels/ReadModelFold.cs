// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Testing.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A configured fold through a scenario's selected read model artifact.
/// </summary>
/// <typeparam name="TReadModel">The read model type.</typeparam>
internal sealed class ReadModelFold<TReadModel>
    where TReadModel : class
{
    /// <summary>
    /// Gets the runtime read model type, including for an untyped fold.
    /// </summary>
    public Type ReadModelType { get; init; } = typeof(TReadModel);

    /// <summary>
    /// Gets the owning store.
    /// </summary>
    public required EventStoreForTesting EventStore { get; init; }

    /// <summary>
    /// Gets the event type registry.
    /// </summary>
    public required IEventTypes EventTypes { get; init; }

    /// <summary>
    /// Gets the schema generator.
    /// </summary>
    public required IJsonSchemaGenerator SchemaGenerator { get; init; }

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    public required IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// Gets the selected reducer, if any.
    /// </summary>
    public Type? ReducerType { get; init; }

    /// <summary>
    /// Gets the selected projection, if any.
    /// </summary>
    public Contracts.Projections.ProjectionDefinition? ProjectionDefinition { get; init; }

    /// <summary>
    /// Gets the optional initial state.
    /// </summary>
    public TReadModel? InitialState { get; init; }

    /// <summary>
    /// Gets whether unsubscribed input is refused.
    /// </summary>
    public bool StrictEventSubscription { get; init; }

    /// <summary>
    /// Processes input through the selected handler.
    /// </summary>
    /// <param name="events">The routed events.</param>
    /// <param name="eventContexts">Original contexts when folding appended events.</param>
    /// <param name="inputSequenceId">Optional isolated sequence for appended scenario input and key lookups.</param>
    /// <returns>The primary instance and every keyed instance.</returns>
    /// <exception cref="NoReadModelHandlerFound">No handler exists for the read model.</exception>
    public async Task<(TReadModel? Primary, IReadOnlyDictionary<EventSourceId, TReadModel> Instances)> Process(
        IEnumerable<ReadModelEvent> events,
        IReadOnlyList<EventContext>? eventContexts = null,
        EventSequenceId? inputSequenceId = null)
    {
        var eventsList = events.ToList();
        if (eventContexts is null && eventsList.Exists(input => input.Context is not null))
        {
            return await ReadModelAppendedEvents.Process(this, eventsList);
        }

        var context = new ReadModelFoldContext(EventStore, EventTypes, SchemaGenerator, ServiceProvider, ReadModelType)
        {
            EventContexts = eventContexts,
            InputSequenceId = inputSequenceId
        };
        if (ReducerType is not null)
        {
            var (reduced, distinctEventSourceIds) = await ReducerReadModelProcessor.Process(
                ReducerType,
                eventsList.Select(e => new EventForEventSourceId(e.EventSourceId, e.Event, Causation.Unknown())
                {
                    EventSourceType = e.EventSourceType,
                    EventStreamType = e.EventStreamType,
                    EventStreamId = e.EventStreamId,
                    Tags = e.Tags
                }),
                context,
                InitialState);

            var reducerInstances = new Dictionary<EventSourceId, TReadModel>();
            if (reduced is not null && distinctEventSourceIds.Count == 1)
            {
                reducerInstances[distinctEventSourceIds[0]] = reduced;
            }

            return (reduced, reducerInstances);
        }

        if (ProjectionDefinition is not null)
        {
            return await ProjectionReadModelProcessor.Process(
                ProjectionDefinition,
                eventsList.Select(e => (e.EventSourceId, e.Event)),
                context,
                InitialState,
                StrictEventSubscription,
                seededEvents: eventsList);
        }

        throw new NoReadModelHandlerFound(ReadModelType);
    }
}
