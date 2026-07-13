// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an implementation of <see cref="ILoadBalancerStrategy"/> that cycles through the
/// available servers in order.
/// </summary>
/// <param name="startOffset">The offset the rotation starts at - the first selection is the address at this offset modulo the number of addresses.</param>
public class RoundRobinLoadBalancerStrategy(int startOffset) : ILoadBalancerStrategy
{
    /// <summary>
    /// The well-known name of the strategy, usable as the loadBalancer option in a connection string.
    /// </summary>
    public const string StrategyName = "round-robin";

    int _next = startOffset - 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="RoundRobinLoadBalancerStrategy"/> class.
    /// </summary>
    /// <remarks>
    /// The rotation starts at a random offset so that a fleet of client instances connecting once
    /// each spreads across the servers instead of all picking the first one.
    /// </remarks>
    public RoundRobinLoadBalancerStrategy() : this(Random.Shared.Next(0, int.MaxValue))
    {
    }

    /// <inheritdoc/>
    public ChronicleServerAddress Next(IReadOnlyList<ChronicleServerAddress> serverAddresses)
    {
        if (serverAddresses.Count == 0)
        {
            throw new MissingServerAddress();
        }

        var index = (uint)Interlocked.Increment(ref _next) % (uint)serverAddresses.Count;
        return serverAddresses[(int)index];
    }
}
