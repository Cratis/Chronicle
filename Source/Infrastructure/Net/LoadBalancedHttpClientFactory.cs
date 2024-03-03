// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;

namespace Cratis.Net;

/// <summary>
/// Represents an implementation of <see cref="ILoadBalancedHttpClientFactory"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LoadBalancedHttpClientFactory"/> class.
/// </remarks>
/// <param name="strategy">The <see cref="ILoadBalancerStrategy"/> to use.</param>
/// <param name="httpClientFactory">The inner <see cref="IHttpClientFactory"/> being used.</param>
public class LoadBalancedHttpClientFactory(
    ILoadBalancerStrategy strategy,
    IHttpClientFactory httpClientFactory) : ILoadBalancedHttpClientFactory
{
    /// <inheritdoc/>
    public HttpClient Create(IEnumerable<Uri> endpoints, string? name = default)
    {
        var endpointsAsArray = endpoints.ToArray();
        var next = strategy.GetNext(endpointsAsArray.Length);
        var nextEndpoint = endpointsAsArray[next];
        var client = httpClientFactory.CreateClient(name ?? Options.DefaultName);
        client.Timeout = TimeSpan.FromSeconds(300);
        client.BaseAddress = nextEndpoint;
        return client;
    }
}
