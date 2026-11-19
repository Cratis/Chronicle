// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Net;
using System.Text.Json;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_TokenHttpMessageHandler.given;

public class a_signed_token_delivery : Signing.for_WebhookDeliverySigner.given.a_delivery_signer
{
    protected RetryClock _clock;
    protected RecordingReceiver _receiver;
    protected IWebhookDeliverySigner _deliverySigner;
    protected WebhookMessageIdentity? _identity;
    protected WebhookDeliveryResult _result;
    HttpClient _client;
    WebhookMediator _mediator;

    void Establish()
    {
        _clock = new();
        _receiver = new(_clock);
        _deliverySigner = _signer;
        _identity = WebhookMessageIdentity.ForEvents(new("webhook", "store", "namespace", "log"), "partition", [], null);
    }

    protected async Task Deliver()
    {
        var attempt = 0;
        _client = TokenHttpMessageHandler.CreateClient(
            new(_receiver) { BaseAddress = new("https://example.com/webhook") },
            (_, _) => Task.FromResult(new AccessTokenInfo($"token-{++attempt}", DateTimeOffset.MaxValue)),
            _ => { });
        var factory = Substitute.For<IWebhookHttpClientFactory>();
        factory.Create(Arg.Any<WebhookTarget>(), Arg.Any<string?>()).Returns(_client);
        _mediator = new(factory, new JsonSerializerOptions(), signer: _deliverySigner, timeProvider: _clock);
        _result = await _mediator.Deliver("store", new(new("https://example.com/webhook"), WebhookAuthorization.None, new Dictionary<string, string>()), "partition", [], signingIdentity: _identity);
    }

    void Destroy() => _client.Dispose();

    public sealed class RetryClock : TimeProvider
    {
        DateTimeOffset _now = Now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance() => _now += TimeSpan.FromSeconds(5);
    }

    public sealed class RecordingReceiver(RetryClock clock) : HttpMessageHandler
    {
        public List<byte[]> Bodies { get; } = [];
        public List<Dictionary<string, string[]>> Headers { get; } = [];
        public HttpStatusCode FinalStatus { get; set; } = HttpStatusCode.OK;

        public bool Verifies(int index) => WebhookSignatureVerification.Verify(
            Headers[index]["webhook-id"].Single(),
            long.Parse(Headers[index]["webhook-timestamp"].Single(), CultureInfo.InvariantCulture),
            Bodies[index],
            Headers[index]["webhook-signature"].Single(),
            [],
            [PublicKey],
            Headers[index]["Cratis-Key-Id"].Single());

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Bodies.Add(await request.Content!.ReadAsByteArrayAsync(cancellationToken));
            Headers.Add(request.Headers.ToDictionary(header => header.Key, header => header.Value.ToArray(), StringComparer.OrdinalIgnoreCase));
            clock.Advance();
            return new(Headers.Count == 1 ? HttpStatusCode.Unauthorized : FinalStatus);
        }
    }
}
