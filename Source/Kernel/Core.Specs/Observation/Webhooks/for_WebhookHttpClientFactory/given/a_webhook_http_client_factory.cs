// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookHttpClientFactory.given;

public class a_webhook_http_client_factory : Specification
{
    protected WebhookHttpClientFactory _factory;
    protected IHttpClientFactory _httpClientFactory;

    void Establish()
    {
        _httpClientFactory = Substitute.For<IHttpClientFactory>();
        var mockHttpClient = new HttpClient();
        _httpClientFactory.CreateClient(Arg.Any<string>()).Returns(mockHttpClient);

        _factory = new WebhookHttpClientFactory(_httpClientFactory);
    }
}
