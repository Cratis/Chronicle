// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Concepts.Observation.Reducers;
using Cratis.Chronicle.Namespaces;
using Cratis.Chronicle.Recommendations;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Observation.Reactors;
using Cratis.Chronicle.Storage.Observation.Reducers;
using Orleans.Core;

namespace Cratis.Chronicle.Observation.ClientSubscriptions;

/// <summary>
/// Builds the collaborators a client reactor or reducer subscription talks to, so specs can state what the subscription did
/// to the observer without running a silo.
/// </summary>
public static class ClientSubscriptionFakes
{
    /// <summary>
    /// Creates a grain factory handing out the given grains.
    /// </summary>
    /// <param name="observer">The observer grain every observer key resolves to.</param>
    /// <param name="connectedClients">The connected clients grain every silo resolves to.</param>
    /// <param name="namespaces">The namespaces grain every event store resolves to.</param>
    /// <param name="recommendations">The recommendations manager grain every key resolves to.</param>
    /// <returns>The <see cref="IGrainFactory"/>.</returns>
    public static IGrainFactory GrainFactory(
        IObserver observer,
        IConnectedClients connectedClients,
        INamespaces namespaces,
        IRecommendationsManager? recommendations = default)
    {
        var grainFactory = Substitute.For<IGrainFactory>();
        grainFactory.GetGrain<IObserver>(Arg.Any<string>(), Arg.Any<string?>()).Returns(observer);
        grainFactory.GetGrain<IConnectedClients>(Arg.Any<string>(), Arg.Any<string?>()).Returns(connectedClients);
        grainFactory.GetGrain<INamespaces>(Arg.Any<string>(), Arg.Any<string?>()).Returns(namespaces);
        var management = Substitute.For<IManagementGrain>();
        management.GetHosts(true).Returns(new Dictionary<SiloAddress, SiloStatus>());
        grainFactory.GetGrain<IManagementGrain>(Arg.Any<long>(), Arg.Any<string?>()).Returns(management);
        if (recommendations is not null)
        {
            grainFactory.GetGrain<IRecommendationsManager>(Arg.Any<long>(), Arg.Any<string>(), Arg.Any<string?>()).Returns(recommendations);
        }

        return grainFactory;
    }

    /// <summary>
    /// Creates storage whose reactor definitions are the state of the given storage, as the shared definition is for every connection.
    /// </summary>
    /// <param name="state">The <see cref="Orleans.Core.IStorage{TState}"/> whose state is the stored definition; a state without event types is no stored definition.</param>
    /// <returns>The <see cref="Storage.IStorage"/>.</returns>
    public static Storage.IStorage StorageFor(IStorage<ReactorDefinition> state)
    {
        var reactors = Substitute.For<IReactorDefinitionsStorage>();
        reactors.Has(Arg.Any<ReactorId>()).Returns(_ => Task.FromResult(state.State?.EventTypes is not null));
        reactors.Get(Arg.Any<ReactorId>()).Returns(_ => Task.FromResult(state.State));
        reactors.Save(Arg.Any<ReactorDefinition>()).Returns(async call =>
        {
            var previous = state.State;
            state.State = call.Arg<ReactorDefinition>();
            try
            {
                await state.WriteStateAsync();
            }
            catch
            {
                state.State = previous;
                throw;
            }
        });

        var eventStore = Substitute.For<IEventStoreStorage>();
        eventStore.Reactors.Returns(reactors);
        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        return storage;
    }

    /// <summary>
    /// Creates storage whose reducer definitions are the state of the given storage, as the shared definition is for every connection.
    /// </summary>
    /// <param name="state">The <see cref="Orleans.Core.IStorage{TState}"/> whose state is the stored definition; a state without event types is no stored definition.</param>
    /// <returns>The <see cref="Storage.IStorage"/>.</returns>
    public static Storage.IStorage StorageFor(IStorage<ReducerDefinition> state)
    {
        var reducers = Substitute.For<IReducerDefinitionsStorage>();
        reducers.Has(Arg.Any<ReducerId>()).Returns(_ => Task.FromResult(state.State?.EventTypes is not null));
        reducers.Get(Arg.Any<ReducerId>()).Returns(_ => Task.FromResult(state.State));
        reducers.Save(Arg.Any<ReducerDefinition>()).Returns(async call =>
        {
            var previous = state.State;
            state.State = call.Arg<ReducerDefinition>();
            try
            {
                await state.WriteStateAsync();
            }
            catch
            {
                state.State = previous;
                throw;
            }
        });

        var eventStore = Substitute.For<IEventStoreStorage>();
        eventStore.Reducers.Returns(reducers);
        var storage = Substitute.For<Storage.IStorage>();
        storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        return storage;
    }
}
