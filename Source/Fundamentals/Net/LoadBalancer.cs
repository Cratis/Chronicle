// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Net;

/// <summary>
/// Represents an implementation of <see cref="ILoadBalancer"/>.
/// </summary>
public class LoadBalancer : ILoadBalancer
{
    readonly IHttpClientFactory _httpClientFactory;

    public LoadBalancer(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc/>
    public ILoadBalancedHttpClientFactory CreateHttpClientFactory(ILoadBalancerStrategy strategy)
        => new LoadBalancedHttpClientFactory(strategy, _httpClientFactory);
}
