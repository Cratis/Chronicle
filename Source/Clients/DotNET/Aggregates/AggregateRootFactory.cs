// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Auditing;
using Aksio.Cratis.Events;
using Aksio.Cratis.EventSequences;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootFactory"/>.
/// </summary>
public class AggregateRootFactory : IAggregateRootFactory
{
    readonly IDictionary<Type, IAggregateRootEventHandlers> _eventHandlersByAggregateRootType = new Dictionary<Type, IAggregateRootEventHandlers>();
    readonly IAggregateRootStateProviders _aggregateRootStateProviders;
    readonly IAggregateRootEventHandlersFactory _aggregateRootEventHandlersFactory;
    readonly ICausationManager _causationManager;
    readonly IEventStore _eventStore;
    readonly IEventSerializer _eventSerializer;
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRootFactory"/> class.
    /// </summary>
    /// <param name="aggregateRootStateProviders"><see cref="IAggregateRootStateProvider"/> for managing state for an aggregate root.</param>
    /// <param name="aggregateRootEventHandlersFactory"><see cref="IAggregateRootEventHandlersFactory"/> for creating <see cref="IAggregateRootEventHandlers"/>.</param>
    /// <param name="causationManager">The <see cref="ICausationManager"/> for handling causation.</param>
    /// <param name="eventStore"><see cref="IEventStore"/> to get event sequence to work with.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for creating instances.</param>
    public AggregateRootFactory(
        IAggregateRootStateProviders aggregateRootStateProviders,
        IAggregateRootEventHandlersFactory aggregateRootEventHandlersFactory,
        ICausationManager causationManager,
        IEventStore eventStore,
        IEventSerializer eventSerializer,
        IServiceProvider serviceProvider)
    {
        _aggregateRootStateProviders = aggregateRootStateProviders;
        _aggregateRootEventHandlersFactory = aggregateRootEventHandlersFactory;
        _causationManager = causationManager;
        _eventStore = eventStore;
        _eventSerializer = eventSerializer;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public async Task<T> Get<T>(EventSourceId id)
        where T : IAggregateRoot
    {
        var aggregateRoot = ActivatorUtilities.CreateInstance<T>(_serviceProvider);
        var eventHandlers = GetEventHandlers(aggregateRoot);

        var eventSequence = _eventStore.GetEventSequence(aggregateRoot.EventSequenceId);

        if (aggregateRoot is AggregateRoot knownAggregateRoot)
        {
            var stateProvider = await _aggregateRootStateProviders.CreateFor(knownAggregateRoot);
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
        aggregateRoot.CausationManager = _causationManager;
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
            eventHandlers = _aggregateRootEventHandlersFactory.CreateFor(aggregateRoot);
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
                var @event = await _eventSerializer.Deserialize(_);
                return new EventAndContext(@event, _.Context);
            }).ToArray();

            var deserializedEvents = await Task.WhenAll(deserializedEventsTasks);
            await eventHandlers.Handle(aggregateRoot, deserializedEvents);
        }
    }
}
