// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Grains.EventSequences;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

/// <summary>
/// Represents an implementation of <see cref="IReactorObserverSubscriber{TReactor}"/> for kernel reactors.
/// </summary>
/// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing and deserializing events.</param>
/// <typeparam name="TReactor">The type of reactor that will be used.</typeparam>
public class ReactorObserverSubscriber<TReactor>(IEventSerializer eventSerializer) : Grain, IReactorObserverSubscriber<TReactor>
    where TReactor : IReactor
{
    TReactor? _reactor;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _reactor = ServiceProvider.GetRequiredService<TReactor>();
        _reactor.Initialize(eventSerializer);
        return base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context) =>
        _reactor?.OnNext(events) ?? Task.FromResult(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Reactor not initialized."));
}
