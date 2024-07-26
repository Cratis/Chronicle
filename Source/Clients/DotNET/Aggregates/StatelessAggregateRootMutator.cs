// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootMutator"/> for stateless <see cref="IAggregateRoot"/>.
/// </summary>
/// <param name="aggregateRootContext">The <see cref="IAggregateRootContext"/> to work with.</param>
/// <param name="eventStoreName">The <see cref="EventStoreName"/> for the aggregate root.</param>
/// <param name="eventStoreNamespaceName">The <see cref="EventStoreNamespaceName"/> for the aggregate root.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
/// <param name="eventHandlers">The <see cref="IAggregateRootEventHandlers"/> for the aggregate root.</param>
public class StatelessAggregateRootMutator(
    IAggregateRootContext aggregateRootContext,
    EventStoreName eventStoreName,
    EventStoreNamespaceName eventStoreNamespaceName,
    IEventSerializer eventSerializer,
    IAggregateRootEventHandlers eventHandlers) : IAggregateRootMutator
{
    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        if (eventHandlers.HasHandleMethods)
        {
            var events = await GetEvents();
            var deserializedEventsTasks = events.Select(async _ =>
            {
                var @event = await eventSerializer.Deserialize(_);
                return new EventAndContext(@event, _.Context);
            }).ToArray();

            var deserializedEvents = await Task.WhenAll(deserializedEventsTasks);
            await eventHandlers.Handle(aggregateRootContext.AggregateRoot, deserializedEvents);
        }
    }

    /// <inheritdoc/>
    public async Task Mutate(object @event)
    {
        await eventHandlers.Handle(
            aggregateRootContext.AggregateRoot,
            [
                new EventAndContext(
                        @event,
                        EventContext.From(
                            eventStoreName,
                            eventStoreNamespaceName,
                            aggregateRootContext.EventSourceId,
                            EventSequenceNumber.Unavailable))
            ]);
    }

    /// <inheritdoc/>
    public Task Dehydrate() => Task.CompletedTask;

    async Task<IImmutableList<AppendedEvent>> GetEvents()
    {
        IImmutableList<AppendedEvent> events = ImmutableList<AppendedEvent>.Empty;
        if (eventHandlers.HasHandleMethods)
        {
            events = await aggregateRootContext.EventSequence.GetForEventSourceIdAndEventTypes(aggregateRootContext.EventSourceId, eventHandlers.EventTypes);
        }

        return events;
    }
}
