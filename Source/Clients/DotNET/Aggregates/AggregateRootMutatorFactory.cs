// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Reflection;

namespace Cratis.Chronicle.Aggregates;

/// <summary>
/// Represents an implementation of <see cref="IAggregateRootMutatorFactory"/>.
/// </summary>
/// <param name="eventStore"><see cref="IEventStore"/> to get event sequence to work with.</param>
/// <param name="aggregateRootStateProviders"><see cref="IAggregateRootStateProviders"/> for creating state providers.</param>
/// <param name="aggregateRootEventHandlersFactory"><see cref="IAggregateRootEventHandlersFactory"/> for creating event handlers.</param>
/// <param name="eventSerializer"><see cref="IEventSerializer"/> for serializing and deserializing events.</param>
public class AggregateRootMutatorFactory(
    IEventStore eventStore,
    IAggregateRootStateProviders aggregateRootStateProviders,
    IAggregateRootEventHandlersFactory aggregateRootEventHandlersFactory,
    IEventSerializer eventSerializer) : IAggregateRootMutatorFactory
{
    /// <inheritdoc/>
    public async Task<IAggregateRootMutator> Create<TAggregateRoot>(IAggregateRootContext context)
    {
        IAggregateRootMutator mutator = null!;

        if (typeof(TAggregateRoot).IsDerivedFromOpenGeneric(typeof(AggregateRoot<>)))
        {
            var baseType = GetGenericAggregateRootType<TAggregateRoot>();
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
            var eventHandlers = aggregateRootEventHandlersFactory.GetFor(context.AggregateRoot);
            mutator = new StatelessAggregateRootMutator(context, eventStore, eventSerializer, eventHandlers);
        }

        return mutator;
    }

    Type? GetGenericAggregateRootType<TAggregateRoot>() => typeof(TAggregateRoot)
                                .AllBaseAndImplementingTypes()
                                .SingleOrDefault(type =>
                                    type.IsGenericType &&
                                    !type.IsGenericTypeDefinition &&
                                    type.GetGenericTypeDefinition() == typeof(AggregateRoot<>));

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
