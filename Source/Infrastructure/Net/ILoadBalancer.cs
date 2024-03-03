// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Net;

/// <summary>
/// Defines a load balancer entry point.
/// </summary>
public interface ILoadBalancer
{
    /// <summary>
    /// Create a <see cref="ILoadBalancedHttpClientFactory"/> with a specific <see cref="ILoadBalancerStrategy"/>.
    /// </summary>
    /// <param name="strategy"><see cref="ILoadBalancerStrategy"/> to use.</param>
    /// <returns>A new <see cref="ILoadBalancedHttpClientFactory"/>.</returns>
    ILoadBalancedHttpClientFactory CreateHttpClientFactory(ILoadBalancerStrategy strategy);
}
