// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Reactors;
using Microsoft.Extensions.DependencyInjection;
using Orleans.Providers;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

/// <summary>
/// Represents an implementation of <see cref="IReactorObserverSubscriber{TReactor}"/> for kernel reactors.
/// </summary>
/// <typeparam name="TReactor">The type of reactor that will be used.</typeparam>
[StorageProvider(ProviderName = WellKnownGrainStorageProviders.Reactors)]
public class ReactorObserverSubscriber<TReactor> : Grain<ReactorDefinition>, IReactorObserverSubscriber<TReactor>
    where TReactor : IReactor
{
    /// <inheritdoc/>
    public Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        var reactor = ServiceProvider.GetRequiredService<TReactor>();
        return reactor.OnNext(events);
    }
}
