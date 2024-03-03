// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Net;

/// <summary>
/// Represents an implementation of <see cref="ILoadBalancer"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LoadBalancer"/> class.
/// </remarks>
/// <param name="httpClientFactory"><see cref="IHttpClientFactory"/> to use for the <see cref="ILoadBalancedHttpClientFactory"/>.</param>
public class LoadBalancer(IHttpClientFactory httpClientFactory) : ILoadBalancer
{
    /// <inheritdoc/>
    public ILoadBalancedHttpClientFactory CreateHttpClientFactory(ILoadBalancerStrategy strategy)
        => new LoadBalancedHttpClientFactory(strategy, httpClientFactory);
}
