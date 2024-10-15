// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AggregateRootFactory"/> class.
/// </remarks>
/// <param name="eventStore"><see cref="IEventStore"/> to get event sequence to work with.</param>
/// <param name="mutatorFactory"><see cref="IAggregateRootMutatorFactory"/> for creating mutators.</param>
/// <param name="unitOfWorkManager"><see cref="IUnitOfWorkManager"/> for managing units of work.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for creating instances.</param>
public class AggregateRootFactory(
    IEventStore eventStore,
    IAggregateRootMutatorFactory mutatorFactory,
    IUnitOfWorkManager unitOfWorkManager,
    IServiceProvider serviceProvider) : IAggregateRootFactory
{
    /// <inheritdoc/>
    public async Task<TAggregateRoot> Get<TAggregateRoot>(EventSourceId id, EventStreamId? streamId = default, EventSource? eventSource = default)
        where TAggregateRoot : IAggregateRoot
    {
        // TODO: Create Issue: Must dispose of unit of work in some way or else it's a memory leak.
        var unitOfWork = unitOfWorkManager.HasCurrent ? unitOfWorkManager.Current : unitOfWorkManager.Begin(CorrelationId.New());

        var aggregateRoot = ActivatorUtilities.CreateInstance<TAggregateRoot>(serviceProvider);
        var eventSequence = eventStore.GetEventSequence(EventSequenceId.Log);
        var eventStreamType = aggregateRoot.GetEventStreamType();
        streamId ??= EventStreamId.Default;
        eventSource ??= EventSource.Default;

        var context = new AggregateRootContext(
            eventSource,
            id,
            eventStreamType,
            streamId,
            eventSequence,
            aggregateRoot,
            unitOfWork,
            EventSequenceNumber.First);
        var mutator = await mutatorFactory.Create<TAggregateRoot>(context);

        await mutator.Rehydrate();

        if (aggregateRoot is AggregateRoot knownAggregateRoot)
        {
            knownAggregateRoot._mutation = new AggregateRootMutation(context, mutator, eventSequence);
            knownAggregateRoot._context = context;
            await knownAggregateRoot.InternalOnActivate();
        }

        return aggregateRoot;
    }
}
