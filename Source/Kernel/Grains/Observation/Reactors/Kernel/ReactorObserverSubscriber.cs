// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Grains.EventTypes.Kernel;
using Cratis.Chronicle.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Grains.Observation.Reactors.Kernel;

/// <summary>
/// Represents an implementation of <see cref="IReactorObserverSubscriber{TReactor}"/> for kernel reactors.
/// </summary>
/// <param name="eventTypes">The <see cref="IEventTypes"/> for working with event types.</param>
/// <param name="expandoObjectConverter">The <see cref="IExpandoObjectConverter"/> for converting between expando objects to and from json.</param>
/// <param name="jsonSerializerOptions">The <see cref="JsonSerializerOptions"/> to use for JSON serialization.</param>
/// <typeparam name="TReactor">The type of reactor that will be used.</typeparam>
public class ReactorObserverSubscriber<TReactor>(
    IEventTypes eventTypes,
    IExpandoObjectConverter expandoObjectConverter,
    JsonSerializerOptions jsonSerializerOptions) : Grain, IReactorObserverSubscriber<TReactor>
    where TReactor : IReactor
{
    TReactor? _reactor;

    /// <inheritdoc/>
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _reactor = ServiceProvider.GetRequiredService<TReactor>();
        _reactor.Initialize(eventTypes, expandoObjectConverter, jsonSerializerOptions);
        return base.OnActivateAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context) =>
        _reactor?.OnNext(events) ?? Task.FromResult(ObserverSubscriberResult.Failed(EventSequenceNumber.Unavailable, "Reactor not initialized."));
}
