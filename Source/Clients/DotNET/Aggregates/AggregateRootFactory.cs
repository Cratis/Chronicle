// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Aksio.Cratis.Client;
using Aksio.Cratis.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Aksio.Cratis.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootFactory"/>.
/// </summary>
public class AggregateRootFactory : IAggregateRootFactory
{
    readonly IDictionary<Type, AggregateRootEventHandlers> _eventHandlersByAggregateRootType = new Dictionary<Type, AggregateRootEventHandlers>();
    readonly IAggregateRootStateManager _aggregateRootStateManager;
    readonly IEventSequences _eventSequences;
    readonly IEventSerializer _eventSerializer;
    readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="AggregateRootFactory"/> class.
    /// </summary>
    /// <param name="aggregateRootStateManager"><see cref="IAggregateRootStateManager"/> for managing state for an aggregate root.</param>
    /// <param name="eventSequences"><see cref="IEventSequences"/> to get event sequence to work with.</param>
    /// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
    /// <param name="serviceProvider"><see cref="IServiceProvider"/> for creating instances.</param>
    public AggregateRootFactory(
        IAggregateRootStateManager aggregateRootStateManager,
        IEventSequences eventSequences,
        IEventSerializer eventSerializer,
        IServiceProvider serviceProvider)
    {
        _aggregateRootStateManager = aggregateRootStateManager;
        _eventSequences = eventSequences;
        _eventSerializer = eventSerializer;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc/>
    public async Task<T> GetFor<T>(EventSourceId id)
        where T : IAggregateRoot
    {
        var aggregateRoot = _serviceProvider.GetRequiredService<T>();
        if (!_eventHandlersByAggregateRootType.TryGetValue(typeof(T), out var eventHandlers))
        {
            eventHandlers = new AggregateRootEventHandlers(typeof(T));
            _eventHandlersByAggregateRootType[typeof(T)] = eventHandlers;
        }

        var eventSequence = _eventSequences.GetEventSequence(aggregateRoot.EventSequenceId);
        IImmutableList<AppendedEvent> events = ImmutableList<AppendedEvent>.Empty;
        if (aggregateRoot is AggregateRoot knownAggregateRoot)
        {
            knownAggregateRoot.EventHandlers = eventHandlers;
            knownAggregateRoot.EventSequence = eventSequence;
            knownAggregateRoot.EventSourceId = id;

            if (knownAggregateRoot.IsStateful)
            {
                events = await eventSequence.GetForEventSourceId(id);
                await _aggregateRootStateManager.Handle(knownAggregateRoot, events);
            }
        }

        if (!eventHandlers.HasHandleMethods)
        {
            return aggregateRoot;
        }

        if (events.Count == 0)
        {
            events = await eventSequence.GetForEventSourceId(id);
        }
        var deserializedEvents = events.Select(_ =>
        {
            var @event = _eventSerializer.Deserialize(_);
            return new EventAndContext(@event, _.Context);
        }).ToArray();

        await eventHandlers.Handle(aggregateRoot, deserializedEvents);

        return aggregateRoot;
    }
}
