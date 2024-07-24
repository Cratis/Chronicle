// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AggregateRootFactory"/> class.
/// </remarks>
/// <param name="eventStore"><see cref="IEventStore"/> to get event sequence to work with.</param>
/// <param name="aggregateRootStateProviders"><see cref="IAggregateRootStateProvider"/> for managing state for an aggregate root.</param>
/// <param name="aggregateRootEventHandlersFactory"><see cref="IAggregateRootEventHandlersFactory"/> for creating <see cref="IAggregateRootEventHandlers"/>.</param>
/// <param name="causationManager">The <see cref="ICausationManager"/> for handling causation.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for creating instances.</param>
public class AggregateRootFactory(
    IEventStore eventStore,
    IAggregateRootStateProviders aggregateRootStateProviders,
    IAggregateRootEventHandlersFactory aggregateRootEventHandlersFactory,
    ICausationManager causationManager,
    IEventSerializer eventSerializer,
    IServiceProvider serviceProvider) : IAggregateRootFactory
{
    readonly IDictionary<Type, IAggregateRootEventHandlers> _eventHandlersByAggregateRootType = new Dictionary<Type, IAggregateRootEventHandlers>();

    /// <inheritdoc/>
    public async Task<T> Get<T>(EventSourceId id)
        where T : IAggregateRoot
    {
        var aggregateRoot = ActivatorUtilities.CreateInstance<T>(serviceProvider);
        var eventHandlers = GetEventHandlers(aggregateRoot);

        var eventSequence = eventStore.GetEventSequence(aggregateRoot.EventSequenceId);

        if (aggregateRoot is AggregateRoot knownAggregateRoot)
        {
            var stateProvider = await aggregateRootStateProviders.CreateFor(knownAggregateRoot);
            SetAggregateRootInternals(
                id,
                eventHandlers,
                stateProvider,
                eventSequence,
                knownAggregateRoot);

            var state = await stateProvider.Provide();
            knownAggregateRoot.MutateState(state);
        }

        if (!aggregateRoot.IsStateful)
        {
            await HandleAnyEventHandlers(aggregateRoot, id, eventHandlers, eventSequence);
        }

        if (aggregateRoot is AggregateRoot aggregateRootToActivate)
        {
            await aggregateRootToActivate.InternalOnActivate();
        }

        return aggregateRoot;
    }

    void SetAggregateRootInternals(
        EventSourceId id,
        IAggregateRootEventHandlers eventHandlers,
        IAggregateRootStateProvider stateProvider,
        IEventSequence eventSequence,
        AggregateRoot aggregateRoot)
    {
        aggregateRoot.EventHandlers = eventHandlers;
        aggregateRoot.StateProvider = stateProvider;
        aggregateRoot.EventSequence = eventSequence;
        aggregateRoot.CausationManager = causationManager;
        aggregateRoot._eventSourceId = id;
    }

    async Task<IImmutableList<AppendedEvent>> GetEvents<T>(EventSourceId id, T aggregateRoot, IAggregateRootEventHandlers eventHandlers, IEventSequence eventSequence)
        where T : IAggregateRoot
    {
        IImmutableList<AppendedEvent> events = ImmutableList<AppendedEvent>.Empty;
        if (aggregateRoot.IsStateful || eventHandlers.HasHandleMethods)
        {
            events = await eventSequence.GetForEventSourceIdAndEventTypes(id, eventHandlers.EventTypes);
        }

        return events;
    }

    IAggregateRootEventHandlers GetEventHandlers<T>(T aggregateRoot)
        where T : IAggregateRoot
    {
        if (!_eventHandlersByAggregateRootType.TryGetValue(typeof(T), out var eventHandlers))
        {
            eventHandlers = aggregateRootEventHandlersFactory.CreateFor(aggregateRoot);
            _eventHandlersByAggregateRootType[typeof(T)] = eventHandlers;
        }

        return eventHandlers;
    }

    async Task HandleAnyEventHandlers<T>(T aggregateRoot, EventSourceId id, IAggregateRootEventHandlers eventHandlers, IEventSequence eventSequence)
        where T : IAggregateRoot
    {
        if (eventHandlers.HasHandleMethods)
        {
            var events = await GetEvents(id, aggregateRoot, eventHandlers, eventSequence);
            var deserializedEventsTasks = events.Select(async _ =>
            {
                var @event = await eventSerializer.Deserialize(_);
                return new EventAndContext(@event, _.Context);
            }).ToArray();

            var deserializedEvents = await Task.WhenAll(deserializedEventsTasks);
            await eventHandlers.Handle(aggregateRoot, deserializedEvents);
        }
    }
}
