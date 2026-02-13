// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Cratis.Chronicle.Storage;
using Cratis.DependencyInjection;
using Cratis.Types;
using Microsoft.CodeAnalysis;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

/// <summary>
/// Represents an implementation of <see cref="IReactors"/>.
/// </summary>
/// <param name="types">The <see cref="ITypes"/> to use for discovering reactors.</param>
/// <param name="localSiloDetails">The local silo details.</param>
/// <param name="storage">The <see cref="IStorage"/> to use for working with underlying storage.</param>
/// <param name="grainFactory">The <see cref="IGrainFactory"/> to use for creating reactor grains.</param>
[Singleton]
public class Reactors(
    ITypes types,
    ILocalSiloDetails localSiloDetails,
    IStorage storage,
    IGrainFactory grainFactory) : IReactors
{
    readonly IEnumerable<Type> _reactorTypes = types.FindMultiple<IReactor>().Where(t => t != typeof(Reactor)).ToArray();

    /// <inheritdoc/>
    public async Task DiscoverAndRegister(EventStoreName eventStore, EventStoreNamespaceName namespaceName)
    {
        var subscribeMethod = GetType().GetMethod(nameof(Subscribe), BindingFlags.Instance | BindingFlags.NonPublic)!;
        foreach (var reactor in _reactorTypes)
        {
            await (subscribeMethod
                .MakeGenericMethod(reactor)
                .Invoke(this, [eventStore, namespaceName])! as Task)!;
        }
    }

    async Task Subscribe<TReactor>(EventStoreName eventStore, EventStoreNamespaceName namespaceName)
        where TReactor : IReactor
    {
        var system = typeof(TReactor).IsSystemEventStoreOnly();
        if (system && eventStore != EventStoreName.System)
        {
            return;
        }

        var defaultNamespaceOnly = typeof(TReactor).IsDefaultNamespaceOnly();
        if (defaultNamespaceOnly && namespaceName != EventStoreNamespaceName.Default)
        {
            return;
        }

        var reactorId = typeof(TReactor).GetReactorId();
        var reactorType = typeof(TReactor);
        var key = new ObserverKey(
            $"$system.{reactorId}",
            eventStore,
            namespaceName,
            reactorType.GetEventSequenceId());

        var reactorDefinition = new ReactorDefinition(
            key.ObserverId,
            ReactorOwner.Kernel,
            reactorType.GetEventSequenceId(),
            reactorType.GetEventTypes().Select(et => new EventTypeWithKeyExpression(et, WellKnownExpressions.EventSourceId)).ToArray(),
            false);
        await storage.GetEventStore(eventStore).Reactors.Save(reactorDefinition);

        var observer = grainFactory.GetGrain<IObserver>(key);
        await observer.Subscribe<IReactorObserverSubscriber<TReactor>>(
            ObserverType.Reactor,
            reactorType.GetEventTypes(),
            localSiloDetails.SiloAddress,
            null,
            false);
    }
}
