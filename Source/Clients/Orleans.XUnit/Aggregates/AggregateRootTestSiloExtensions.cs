// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Aggregates;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.XUnit;
using Cratis.Chronicle.XUnit.Auditing;
using Cratis.Chronicle.XUnit.Events;
using Orleans.TestKit;

namespace Cratis.Chronicle.Orleans.Aggregates;

/// <summary>
/// Represents extension methods for working with testing with <see cref="AggregateRoot"/>.
/// </summary>
public static class AggregateRootTestSiloExtensions
{
    /// <summary>
    /// Create an instance of an <see cref="AggregateRoot"/> for testing.
    /// </summary>
    /// <typeparam name="TAggregateRoot">Type of <see cref="AggregateRoot"/> to create.</typeparam>
    /// <param name="silo"><see cref="TestKitSilo"/> to create the <see cref="AggregateRoot"/> in.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to create the <see cref="AggregateRoot"/> with.</param>
    /// <param name="events">Optional events to apply to the <see cref="AggregateRoot"/>.</param>
    /// <returns>An instance of the <see cref="AggregateRoot"/> created.</returns>
    public static async Task<TAggregateRoot> CreateAggregateRoot<TAggregateRoot>(this TestKitSilo silo, EventSourceId eventSourceId, params object[] events)
        where TAggregateRoot : AggregateRoot, IGrainWithStringKey
    {
        var eventLog = new EventLogForTesting(Defaults.EventTypes, events);
        var causationManager = new CausationManagerForTesting();
        var aggregateRootEventHandlersFactory = new AggregateRootEventHandlersFactory(Defaults.EventTypes);
        silo.AddService(Defaults.EventSerializer);
        silo.AddService(Defaults.EventStore);
        silo.AddService<IEventLog>(eventLog);
        silo.AddService<ICausationManager>(causationManager);
        silo.AddService<IAggregateRootEventHandlersFactory>(aggregateRootEventHandlersFactory);

        var aggregateRoot = await silo.CreateGrainAsync<TAggregateRoot>(eventSourceId);

        if (aggregateRoot.Context is AggregateRootContext context)
        {
            context.AutoCommit = false;
        }

        return aggregateRoot;
    }

    /// <summary>
    /// Create an instance of an <see cref="AggregateRoot{TState}"/> for testing.
    /// </summary>
    /// <typeparam name="TAggregateRoot">Type of <see cref="AggregateRoot{TState}"/> to create.</typeparam>
    /// <typeparam name="TState">Type of state for the <see cref="AggregateRoot{TState}"/>.</typeparam>
    /// <param name="silo"><see cref="TestKitSilo"/> to create the <see cref="AggregateRoot"/> in.</param>
    /// <param name="eventSourceId"><see cref="EventSourceId"/> to create the <see cref="AggregateRoot"/> with.</param>
    /// <param name="initialState">Optional initial state to start the aggregate with.</param>
    /// <returns>An instance of the <see cref="AggregateRoot{TState}"/> created.</returns>
    public static async Task<TAggregateRoot> CreateAggregateRoot<TAggregateRoot, TState>(this TestKitSilo silo, EventSourceId eventSourceId, TState? initialState = default)
        where TAggregateRoot : AggregateRoot<TState>, IGrainWithStringKey
    {
        var eventLog = new EventLogForTesting(Defaults.EventTypes);
        var causationManager = new CausationManagerForTesting();
        var aggregateRootStateProviders = new AggregateRootStateProvidersForTesting();
        silo.AddService<IEventLog>(eventLog);
        silo.AddService<IAggregateRootStateProviders>(aggregateRootStateProviders);
        silo.AddService<ICausationManager>(causationManager);

        var aggregateRoot = await silo.CreateGrainAsync<TAggregateRoot>(eventSourceId);

        if (initialState is not null)
        {
            aggregateRoot.SetState(initialState);
        }

        if (aggregateRoot.Context is AggregateRootContext context)
        {
            context.AutoCommit = false;
        }

        return aggregateRoot;
    }
}
