// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Represents an implementation of <see cref="ILoadBalancerStrategy"/> that selects a random server.
/// </summary>
public class RandomLoadBalancerStrategy : ILoadBalancerStrategy
{
    /// <summary>
    /// The well-known name of the strategy, usable as the loadBalancer option in a connection string.
    /// </summary>
    public const string StrategyName = "random";

    /// <inheritdoc/>
    public ChronicleServerAddress Next(IReadOnlyList<ChronicleServerAddress> serverAddresses)
    {
        if (serverAddresses.Count == 0)
        {
            throw new MissingServerAddress();
        }

        return serverAddresses[Random.Shared.Next(serverAddresses.Count)];
    }
}
