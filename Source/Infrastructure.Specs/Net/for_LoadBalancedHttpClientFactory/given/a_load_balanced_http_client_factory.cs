// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Net.for_LoadBalancedHttpClientFactory.given;

public class a_load_balanced_http_client_factory : Specification
{
    protected LoadBalancedHttpClientFactory _factory;
    protected IHttpClientFactory _httpClientFactory;
    protected ILoadBalancerStrategy _strategy;

    void Establish()
    {
        _strategy = Substitute.For<ILoadBalancerStrategy>();
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        _factory = new(_strategy, _httpClientFactory);
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(Mock.Of<HttpClient>());
    }
}
