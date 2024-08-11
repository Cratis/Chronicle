// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Orleans.Aggregates;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Cratis.Reflection;
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
        var correlationId = RequestContext.Get(Constants.CorrelationIdKey) as CorrelationId;
        if (correlationId is not null &&
            (context.InterfaceMethod.DeclaringType?.HasInterface<IAggregateRoot>() ?? false) &&
            unitOfWorkManager.TryGetFor(correlationId, out var unitOfWork))
        {
            var aggregate = (context.TargetContext.GrainInstance as IAggregateRoot)!;
            var aggregateContextHolder = (aggregate as IAggregateRootContextHolder)!;

            unitOfWorkManager.SetCurrent(unitOfWork);
            var aggregateRootContext = new AggregateRootContext(
                aggregate.GetPrimaryKeyString(),
                eventStore.GetEventSequence(EventSequenceId.Log),
                aggregate,
                unitOfWork);

            await aggregateContextHolder.SetContext(aggregateRootContext);
        }

        await context.Invoke();
    }
}
