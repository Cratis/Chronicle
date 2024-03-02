// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Net;

/// <summary>
/// Represents a <see cref="ILoadBalancerStrategy"/> for round robin.
/// </summary>
public class RoundRobinLoadBalancerStrategy : ILoadBalancerStrategy
{
    int _previous;

    /// <inheritdoc/>
    public int GetNext(int max)
    {
        _previous++;
        return (_previous - 1) % max;
    }
}
