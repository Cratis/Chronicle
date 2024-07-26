// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AggregateRootFactory"/> class.
/// </remarks>
/// <param name="eventStore"><see cref="IEventStore"/> to get event sequence to work with.</param>
/// <param name="aggregateRootStateProviders"><see cref="IAggregateRootStateProviders"/> for managing state for an aggregate root.</param>
/// <param name="aggregateRootEventHandlersFactory"><see cref="IAggregateRootEventHandlersFactory"/> for getting <see cref="IAggregateRootEventHandlers"/>.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing events.</param>
/// <param name="causationManager">The <see cref="ICausationManager"/> for handling causation.</param>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for creating instances.</param>
public class AggregateRootFactory(
    IEventStore eventStore,
    IAggregateRootStateProviders aggregateRootStateProviders,
    IAggregateRootEventHandlersFactory aggregateRootEventHandlersFactory,
    IEventSerializer eventSerializer,
    ICausationManager causationManager,
    IServiceProvider serviceProvider) : IAggregateRootFactory
{
    /// <inheritdoc/>
    public async Task<TAggregateRoot> Get<TAggregateRoot>(EventSourceId id, bool autoCommit = true)
        where TAggregateRoot : IAggregateRoot
    {
        var aggregateRoot = ActivatorUtilities.CreateInstance<TAggregateRoot>(serviceProvider);
        var eventSequence = eventStore.GetEventSequence(aggregateRoot.EventSequenceId);
        var context = new AggregateRootContext(CorrelationId.New(), id, eventSequence, aggregateRoot);

        IAggregateRootMutator mutator = null!;

        if (typeof(TAggregateRoot).IsDerivedFromOpenGeneric(typeof(AggregateRoot<>)))
        {
            var baseType = typeof(TAggregateRoot).AllBaseAndImplementingTypes().SingleOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(AggregateRoot<>));
            if (baseType is not null)
            {
                var stateType = typeof(InternalStatefulFactory<>).MakeGenericType(baseType.GetGenericArguments()[0]);
                var method = stateType.GetMethod(nameof(InternalStatefulFactory<object>.CreateMutator));
                var task = method?.Invoke(null, [aggregateRootStateProviders, context]) as Task<IAggregateRootMutator>;
                mutator = await task!;
            }
        }
        else
        {
            var eventHandlers = aggregateRootEventHandlersFactory.GetFor(aggregateRoot);
            mutator = new StatelessAggregateRootMutator(context, eventStore.Name, eventStore.Namespace, eventSerializer, eventHandlers);
        }

        await mutator.Rehydrate();

        if (aggregateRoot is AggregateRoot knownAggregateRoot)
        {
            knownAggregateRoot._mutation = new AggregateRootMutation(id, mutator, eventSequence, causationManager);
            knownAggregateRoot._context = context;
            await knownAggregateRoot.InternalOnActivate();
        }

        return aggregateRoot;
    }

    static class InternalStatefulFactory<TState>
        where TState : class
    {
        public static async Task<IAggregateRootMutator> CreateMutator(IAggregateRootStateProviders stateProviders, IAggregateRootContext context)
        {
            var stateProvider = await stateProviders.CreateFor<TState>(context);
            var aggregateRootState = new AggregateRootState<TState>();
            return new StatefulAggregateRootMutator<TState>(aggregateRootState, stateProvider);
        }
    }
}
