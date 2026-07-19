// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Configuration;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IObserverSubscriberSelector"/> that delegates to the
/// strategy configured through <see cref="Observers.FanOutStrategy"/>.
/// </summary>
[Singleton]
public class ObserverSubscriberSelector : IObserverSubscriberSelector
{
    readonly IObserverSubscriberSelector _strategy;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObserverSubscriberSelector"/> class.
    /// </summary>
    /// <param name="options"><see cref="IOptions{ChronicleOptions}"/> holding the observers configuration.</param>
    /// <exception cref="UnknownFanOutStrategy">Thrown when the configured strategy is not known.</exception>
    public ObserverSubscriberSelector(IOptions<ChronicleOptions> options)
    {
        var strategyName = options.Value.Observers.FanOutStrategy;
        _strategy = strategyName switch
        {
            null or "" or RoundRobinObserverSubscriberSelector.StrategyName => new RoundRobinObserverSubscriberSelector(),
            RandomObserverSubscriberSelector.StrategyName => new RandomObserverSubscriberSelector(),
            _ => throw new UnknownFanOutStrategy(strategyName)
        };
    }

    /// <inheritdoc/>
    public ObserverSubscriberTarget Select(ObserverSubscription subscription, Key partition) => _strategy.Select(subscription, partition);
}
