// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Orleans.Aggregates;
using Cratis.Chronicle.Transactions;
using IAggregateRoot = Cratis.Chronicle.Orleans.Aggregates.IAggregateRoot;

namespace Cratis.Chronicle.Orleans.Transactions;

/// <summary>
/// Represents a filter for managing units of work for incoming calls.
/// </summary>
/// <param name="unitOfWorkManager">The <see cref="IUnitOfWorkManager"/> to use.</param>
/// <param name="eventStore">The <see cref="IEventStore"/> to use.</param>
public class UnitOfWorkIncomingCallFilter(
    IUnitOfWorkManager unitOfWorkManager,
    IEventStore eventStore) : IIncomingGrainCallFilter
{
    /// <inheritdoc/>
    public async Task Invoke(IIncomingGrainCallContext context)
    {
        if (RequestContext.Get(WellKnownKeys.CorrelationId) is Guid correlationId &&
            context.IsMessageToAggregateRoot() &&
            unitOfWorkManager.TryGetFor(correlationId, out var unitOfWork))
        {
            var aggregate = (context.TargetContext.GrainInstance as IAggregateRoot)!;
            var aggregateContextHolder = (aggregate as IAggregateRootContextHolder)!;

            unitOfWorkManager.SetCurrent(unitOfWork);
            var key = (AggregateRootKey)aggregate.GetPrimaryKeyString();

            var aggregateRootContext = new AggregateRootContext(
                key.EventSourceType,
                key.EventSourceId,
                aggregate.GetEventStreamType(),
                key.EventStreamId,
                eventStore.GetEventSequence(EventSequenceId.Log),
                aggregate,
                unitOfWork,
                aggregateContextHolder.Context?.NextSequenceNumber ?? EventSequenceNumber.First);

            await aggregateContextHolder.SetContext(aggregateRootContext);
        }

        await context.Invoke();
    }
}
