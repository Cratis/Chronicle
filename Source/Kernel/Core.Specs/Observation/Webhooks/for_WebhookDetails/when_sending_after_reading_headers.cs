// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookDetails;

public class when_sending_after_reading_headers : given.stored_headers
{
    ExternalServices.for_ExternalService.given.header_receiver _receiver;

    async Task Because()
    {
        await WebhookDetails.GetWebhooks("store", _storage);
        _receiver = new();
        using var transport = new HttpClient(_receiver);
        var clients = Substitute.For<IHttpClientFactory>();
        clients.CreateClient(WebhookHttpClientFactory.HttpClientName).Returns(transport);
        var factory = new WebhookHttpClientFactory(clients, Substitute.For<IOAuthClient>());
        using var client = factory.Create(_definition.Target);
        using var response = await client.GetAsync(string.Empty);
    }

    [Fact] void should_send_the_first_configured_value() => _receiver.Headers["x-first"].ShouldEqual("synthetic-first");
    [Fact] void should_send_the_second_configured_value() => _receiver.Headers["x-second"].ShouldEqual("synthetic-second");
    [Fact] void should_keep_dedicated_authorization() => _receiver.Headers["Authorization"].ShouldEqual("Bearer synthetic-authorization");
}
