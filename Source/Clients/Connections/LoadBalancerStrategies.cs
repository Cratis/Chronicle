// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Factory for creating <see cref="ILoadBalancerStrategy"/> instances from their well-known names.
/// </summary>
public static class LoadBalancerStrategies
{
    /// <summary>
    /// Creates an <see cref="ILoadBalancerStrategy"/> from its well-known name.
    /// </summary>
    /// <param name="name">The name of the strategy. When not specified, defaults to round-robin.</param>
    /// <returns>The <see cref="ILoadBalancerStrategy"/> for the name.</returns>
    /// <exception cref="UnknownLoadBalancerStrategy">Thrown when the name does not match a known strategy.</exception>
    public static ILoadBalancerStrategy Create(string? name = null) =>
        name switch
        {
            null or "" or RoundRobinLoadBalancerStrategy.StrategyName => new RoundRobinLoadBalancerStrategy(),
            RandomLoadBalancerStrategy.StrategyName => new RandomLoadBalancerStrategy(),
            _ => throw new UnknownLoadBalancerStrategy(name)
        };
}
