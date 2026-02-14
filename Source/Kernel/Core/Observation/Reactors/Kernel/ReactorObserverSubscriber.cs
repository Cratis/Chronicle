// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Grains.EventSequences;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Observation.Reactors.Kernel;

/// <summary>
/// Represents an implementation of <see cref="IReactorObserverSubscriber{TReactor}"/> for kernel reactors.
/// </summary>
/// <param name="eventSerializer">The <see cref="IEventSerializer"/> for serializing and deserializing events.</param>
/// <param name="loggerFactory">The <see cref="ILoggerFactory"/> for creating loggers.</param>
/// <typeparam name="TReactor">The type of reactor that will be used.</typeparam>
public class ReactorObserverSubscriber<TReactor>(IEventSerializer eventSerializer, ILoggerFactory loggerFactory) : Grain, IReactorObserverSubscriber<TReactor>
    where TReactor : IReactor
{
    readonly ILogger _logger = loggerFactory.CreateLogger<ReactorObserverSubscriber<TReactor>>();
    TReactor? _reactor;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.ReactorObserverSubscriberActivating(typeof(TReactor).Name);
            _reactor = ServiceProvider.GetRequiredService<TReactor>();
            _reactor.Initialize(eventSerializer);
            _logger.ReactorObserverSubscriberActivated(typeof(TReactor).Name);
        }
        catch (Exception ex)
        {
            _logger.FailedToActivateReactorObserverSubscriber(typeof(TReactor).Name, ex);
            throw;
        }

        return base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context) =>
        _reactor?.OnNext(events) ?? Task.FromResult(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Reactor not initialized."));
}
