// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookHttpClientFactory.when_creating;

public class and_custom_header_values_are_sent : Specification
{
    IHttpClientFactory _transport;
    HttpClient _inner;
    WebhookHttpClientFactory _factory;
    string _sentValue;

    void Establish()
    {
        _transport = Substitute.For<IHttpClientFactory>();
        _inner = new(new Receiver(value => _sentValue = value));
        _transport.CreateClient(Arg.Any<string>()).Returns(_inner);
        _factory = new(_transport, Substitute.For<IOAuthClient>());
    }

    async Task Because()
    {
        using var client = _factory.Create(new("https://receiver.example.com", WebhookAuthorization.None, new Dictionary<string, string> { ["X-First"] = header_events.Sentinels[0] }));
        using var response = await client.GetAsync("/");
    }

    [Fact] void should_send_real_values_as_http_headers() => _sentValue.ShouldEqual(header_events.Sentinels[0]);

    void Destroy() => _inner.Dispose();

    class Receiver(Action<string> receive) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            receive(request.Headers.GetValues("X-First").Single());
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
