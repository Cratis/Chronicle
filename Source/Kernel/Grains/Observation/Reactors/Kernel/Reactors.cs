// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Types;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

/// <summary>
/// Represents an implementation of <see cref="IReactors"/>.
/// </summary>
/// <param name="types">The <see cref="ITypes"/> to use for discovering reactors.</param>
/// <param name="localSiloDetails">The local silo details.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> to use for creating reactor grains.</param>
public class Reactors(
    ITypes types,
    ILocalSiloDetails localSiloDetails,
    IGrainFactory grainFactory) : IReactors
{
    /// <inheritdoc/>
    public async Task DiscoverAndRegister(EventStoreName eventStore, EventStoreNamespaceName namespaceName)
    {
        var subscribeMethod = GetType().GetMethod(nameof(Subscribe), BindingFlags.Instance | BindingFlags.NonPublic)!;
        foreach (var reactor in types.FindMultiple<IReactor>().Where(t => t != typeof(Reactor)))
        {
            await (subscribeMethod
                .MakeGenericMethod(reactor)
                .Invoke(this, [eventStore, namespaceName])! as Task)!;
        }
    }

    async Task Subscribe<TReactor>(EventStoreName eventStore, EventStoreNamespaceName namespaceName)
        where TReactor : IReactor
    {
        var key = new ObserverKey(
            typeof(TReactor).Name,
            eventStore,
            namespaceName,
            EventSequenceId.System);

        var observer = grainFactory.GetGrain<IObserver>(key);
        await observer.Subscribe<IReactorObserverSubscriber<TReactor>>(
            ObserverType.Reactor,
            ObserverOwner.Kernel,
            typeof(TReactor).GetEventTypes(),
            localSiloDetails.SiloAddress,
            null,
            false);
    }
}
