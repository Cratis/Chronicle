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
/// <param name="correlationIdAccessor">The <see cref="ICorrelationIdAccessor"/> for correlation id.</param>
public class StatelessAggregateRootMutator(
    IAggregateRootContext aggregateRootContext,
    IEventStore eventStore,
    IEventSerializer eventSerializer,
    IAggregateRootEventHandlers eventHandlers,
    ICorrelationIdAccessor correlationIdAccessor) : IAggregateRootMutator
{
    /// <inheritdoc/>
    public async Task Rehydrate()
    {
        var events = await aggregateRootContext.EventSequence.GetFromSequenceNumber(aggregateRootContext.NextSequenceNumber, aggregateRootContext.EventSourceId, eventHandlers.EventTypes);
        if (eventHandlers.HasHandleMethods)
        {
            var deserializedEventsTasks = events.Select(async _ =>
            {
                var @event = await eventSerializer.Deserialize(_);
                return new EventAndContext(@event, _.Context);
            }).ToArray();

            var deserializedEvents = await Task.WhenAll(deserializedEventsTasks);

            // Scenario is when state is partially modified before throwing an exception.
            await eventHandlers.Handle(aggregateRootContext.AggregateRoot, deserializedEvents, handledEventAndContext =>
            {
                var nextSequenceNumber = handledEventAndContext.Context.SequenceNumber.Next();
                if (handledEventAndContext.Context.SequenceNumber == EventSequenceNumber.Unavailable)
                {
                    return;
                }
                if (nextSequenceNumber.IsActualValue && nextSequenceNumber > aggregateRootContext.NextSequenceNumber)
                {
                    aggregateRootContext.HasEvents = true;
                    aggregateRootContext.NextSequenceNumber = nextSequenceNumber;
                }
            });
        }

        // We should look at improving how we set the NextSequenceNumber and instead rely only on that, perhaps by returning the last event sequence number. https://github.com/Cratis/Chronicle/issues/1400
        if (aggregateRootContext.NextSequenceNumber == EventSequenceNumber.First)
        {
            var lastHandledEventSequenceNumber = await aggregateRootContext.EventSequence.GetTailSequenceNumber(aggregateRootContext.EventSourceId);
            if (lastHandledEventSequenceNumber.IsActualValue)
            {
                aggregateRootContext.TailEventSequenceNumber = lastHandledEventSequenceNumber;
                aggregateRootContext.HasEvents = true;
            }
        }
    }

    /// <inheritdoc/>
    public async Task Mutate(object @event)
    {
        if (eventHandlers.HasHandleMethods)
        {
            aggregateRootContext.HasEvents = true;
            await eventHandlers.Handle(
                aggregateRootContext.AggregateRoot,
                [
                    new EventAndContext(
                        @event,
                        EventContext.From(
                            eventStore.Name,
                            eventStore.Namespace,
                            aggregateRootContext.EventSourceType,
                            aggregateRootContext.EventSourceId,
                            aggregateRootContext.EventStreamType,
                            aggregateRootContext.EventStreamId,
                            EventSequenceNumber.Unavailable,
                            correlationIdAccessor.Current))
                ]);
        }
    }

    /// <inheritdoc/>
    public Task Dehydrate() => Task.CompletedTask;
}
