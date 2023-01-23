// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Net.for_LoadBalancedHttpClientFactory;

public class when_creating : given.a_load_balanced_http_client_factory
{
    HttpClient result;
    IEnumerable<Uri> endpoints;
    int next;

    void Establish()
    {
        endpoints = Enumerable.Range(0, 5).Select(_ => new Uri($"http://{Guid.NewGuid()}")).ToArray().AsEnumerable();
        next = Random.Shared.Next(0, endpoints.Count());
        strategy.Setup(_ => _.GetNext(endpoints.Count())).Returns(next);
    }

    void Because() => result = factory.Create(endpoints);

    [Fact] void should_return_client_with_correct_endpoint() => result.BaseAddress.ShouldEqual(endpoints.ToArray()[next]);
}
