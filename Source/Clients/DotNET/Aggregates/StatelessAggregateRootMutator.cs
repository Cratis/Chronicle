// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootMutator"/> for stateless <see cref="IAggregateRoot"/>.
/// </summary>
/// <param name="aggregateRootContext">The <see cref="IAggregateRootContext"/> to work with.</param>
/// <param name="eventStore">The <see cref="IEventStore"/> to work with.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
/// <param name="eventHandlers">The <see cref="IAggregateRootEventHandlers"/> for the aggregate root.</param>
public class StatelessAggregateRootMutator(
    IAggregateRootContext aggregateRootContext,
    IEventStore eventStore,
    IEventSerializer eventSerializer,
    IAggregateRootEventHandlers eventHandlers) : IAggregateRootMutator
{
    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        if (eventHandlers.HasHandleMethods)
        {
            var events = await aggregateRootContext.EventSequence.GetForEventSourceIdAndEventTypes(aggregateRootContext.EventSourceId, eventHandlers.EventTypes);
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
        if (eventHandlers.HasHandleMethods)
        {
            await eventHandlers.Handle(
                aggregateRootContext.AggregateRoot,
                [
                    new EventAndContext(
                        @event,
                        EventContext.From(
                            eventStore.Name,
                            eventStore.Namespace,
                            aggregateRootContext.EventSourceId,
                            EventSequenceNumber.Unavailable))
                ]);
        }
    }

    /// <inheritdoc/>
    public Task Dehydrate() => Task.CompletedTask;
}
