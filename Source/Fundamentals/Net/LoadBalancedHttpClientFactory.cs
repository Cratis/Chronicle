// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Net;

/// <summary>
/// Represents an implementation of <see cref="ILoadBalancedHttpClientFactory"/>.
/// </summary>
public class LoadBalancedHttpClientFactory : ILoadBalancedHttpClientFactory
{
    readonly ILoadBalancerStrategy _strategy;
    readonly IHttpClientFactory _httpClientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoadBalancedHttpClientFactory"/> class.
    /// </summary>
    /// <param name="strategy">The <see cref="ILoadBalancerStrategy"/> to use.</param>
    /// <param name="httpClientFactory">The inner <see cref="IHttpClientFactory"/> being used.</param>
    public LoadBalancedHttpClientFactory(
        ILoadBalancerStrategy strategy,
        IHttpClientFactory httpClientFactory)
    {
        _strategy = strategy;
        _httpClientFactory = httpClientFactory;
    }

    /// <inheritdoc/>
    public HttpClient Create(IEnumerable<Uri> endpoints, string? name = default)
    {
        var endpointsAsArray = endpoints.ToArray();
        var next = _strategy.GetNext(endpointsAsArray.Length);
        var nextEndpoint = endpointsAsArray[next];
        var client = _httpClientFactory.CreateClient(name);
        client.BaseAddress = nextEndpoint;
        return client;
    }
}
