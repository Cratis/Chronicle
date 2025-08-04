// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

/// <summary>
/// Represents an implementation of <see cref="IReactorObserverSubscriber{TReactor}"/> for kernel reactors.
/// </summary>
/// <typeparam name="TReactor">The type of reactor that will be used.</typeparam>
public class ReactorObserverSubscriber<TReactor> : Grain, IReactorObserverSubscriber<TReactor>
    where TReactor : IReactor
{
    /// <inheritdoc/>
    public Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context)
    {
        var reactor = ServiceProvider.GetRequiredService<TReactor>();
        return reactor.OnNext(events);
    }
}
