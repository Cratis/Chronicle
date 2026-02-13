// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Grains.Observation;

/// <summary>
/// Represents a null <see cref="IObserverSubscriber"/>.
/// </summary>
public class NullObserverSubscriber : IObserverSubscriber
{
    /// <inheritdoc/>
    public Task<ObserverSubscriberResult> OnNext(Key partition, IEnumerable<AppendedEvent> events, ObserverSubscriberContext context) =>
        Task.FromResult(ObserverSubscriberResult.Disconnected());
}
