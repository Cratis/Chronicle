// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Net;

/// <summary>
/// Represents an implementation of <see cref="ILoadBalancer"/>.
/// </summary>
public class LoadBalancer : ILoadBalancer
{
    readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadBalancer"/> class.
    /// </summary>
    /// <param name="httpClientFactory"><see cref="IHttpClientFactory"/> to use for the <see cref="ILoadBalancedHttpClientFactory"/>.</param>
    public LoadBalancer(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc/>
    public ILoadBalancedHttpClientFactory CreateHttpClientFactory(ILoadBalancerStrategy strategy)
        => new LoadBalancedHttpClientFactory(strategy, _httpClientFactory);
}
