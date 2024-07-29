// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
/// <param name="mutatorFactory"><see cref="IAggregateRootMutatorFactory"/> for creating mutators.</param>
/// <param name="causationManager">The <see cref="ICausationManager"/> for handling causation.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for creating instances.</param>
public class AggregateRootFactory(
    IEventStore eventStore,
    IAggregateRootMutatorFactory mutatorFactory,
    ICausationManager causationManager,
    IServiceProvider serviceProvider) : IAggregateRootFactory
{
    /// <inheritdoc/>
    public async Task<TAggregateRoot> Get<TAggregateRoot>(EventSourceId id, bool autoCommit = true)
        where TAggregateRoot : IAggregateRoot
    {
        var aggregateRoot = ActivatorUtilities.CreateInstance<TAggregateRoot>(serviceProvider);
        var eventSequence = eventStore.GetEventSequence(EventSequenceId.Log);

        // TODO: Fix CorrelationId to be a real value from the current context
        var context = new AggregateRootContext(CorrelationId.New(), id, eventSequence, aggregateRoot, autoCommit);
        var mutator = await mutatorFactory.Create<TAggregateRoot>(context);

        await mutator.Rehydrate();

        if (aggregateRoot is AggregateRoot knownAggregateRoot)
        {
            knownAggregateRoot._mutation = new AggregateRootMutation(context, mutator, eventSequence, causationManager);
            knownAggregateRoot._context = context;
            await knownAggregateRoot.InternalOnActivate();
        }

        return aggregateRoot;
    }
}
