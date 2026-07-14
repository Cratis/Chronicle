// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverSubscriberSelector"/> that selects a random
/// connected client instance for every delivery.
/// </summary>
/// <remarks>
/// Events for a partition are still delivered in order - one batch at a time - but consecutive
/// batches for the same partition can go to different client instances. Use the round-robin
/// strategy when a partition must stay sticky to one instance.
/// </remarks>
public class RandomObserverSubscriberSelector : IObserverSubscriberSelector
{
    /// <summary>
    /// The well-known name of the strategy, usable as the observers fanOutStrategy configuration value.
    /// </summary>
    public const string StrategyName = "random";

    /// <inheritdoc/>
    public ObserverSubscriberTarget Select(ObserverSubscription subscription, Key partition) =>
        subscription.Targets.Count switch
        {
            0 => new(subscription.SiloAddress, null),
            1 => subscription.Targets[0],
            _ => subscription.Targets[Random.Shared.Next(subscription.Targets.Count)]
        };
}
