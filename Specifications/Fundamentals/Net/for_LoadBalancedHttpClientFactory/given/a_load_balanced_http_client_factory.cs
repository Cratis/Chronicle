// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Net.for_LoadBalancedHttpClientFactory.given;

public class a_load_balanced_http_client_factory : Specification
{
    protected LoadBalancedHttpClientFactory factory;
    protected Mock<IHttpClientFactory> http_client_factory;
    protected Mock<ILoadBalancerStrategy> strategy;

    void Establish()
    {
        strategy = new();
        http_client_factory = new();
        factory = new(strategy.Object, http_client_factory.Object);
        http_client_factory.Setup(_ => _.CreateClient(IsAny<string>())).Returns(Mock.Of<HttpClient>());
    }
}
