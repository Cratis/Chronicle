// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Net;

/// <summary>
/// Defines the basis of a load balancer strategy.
/// </summary>
public interface ILoadBalancerStrategy
{
    /// <summary>
    /// Gets the next index to work with.
    /// </summary>
    /// <param name="max">The max number.</param>
    /// <returns>The next index.</returns>
    int GetNext(int max);
}
