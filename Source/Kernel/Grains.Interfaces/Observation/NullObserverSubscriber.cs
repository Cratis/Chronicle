// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Events;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Represents a null <see cref="IObserverSubscriber"/>.
/// </summary>
public class NullObserverSubscriber : IObserverSubscriber
{
    /// <inheritdoc/>
    public Task<ObserverSubscriberResult> OnNext(IEnumerable<AppendedEvent> events, ObserverSubscriberContext context) =>
        Task.FromResult(ObserverSubscriberResult.Disconnected(EventSequenceNumber.Unavailable));
}
