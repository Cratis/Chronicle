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

            // TODO: Create Issue: There is a potential problem here with aggregate root state if for some reason one On method throws an exception, then the next time the aggregate is retrieved it will get that new corrupted state

            // Scenario is when state is partially modified before throwing an exception.
            await eventHandlers.Handle(aggregateRootContext.AggregateRoot, deserializedEvents, _ =>
            {
                var sequenceNumber = _.Context.SequenceNumber;
                if (sequenceNumber != EventSequenceNumber.Unavailable &&
                    sequenceNumber + 1 > aggregateRootContext.NextSequenceNumber)
                {
                    aggregateRootContext.NextSequenceNumber = sequenceNumber + 1;
                }
            });
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
                            EventSequenceNumber.Unavailable,
                            correlationIdAccessor.Current))
                ]);
        }
    }

    /// <inheritdoc/>
    public Task Dehydrate() => Task.CompletedTask;
}
