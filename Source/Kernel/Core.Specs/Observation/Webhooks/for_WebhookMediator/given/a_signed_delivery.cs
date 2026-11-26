// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Globalization;
using System.Net;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Observation.Webhooks.Signing;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.given;

public class a_signed_delivery : a_webhook_mediator_with_recorded_logs
{
    protected static readonly byte[] _seed = Convert.FromHexString("9d61b19deffd5a60ba844af492ec2cc44449c5697b326919703bac031cae7f60");
    protected static readonly byte[] _publicKey = Convert.FromHexString("d75a980182b10ab7d54bfed3c964073a0ee172f3daa62325af021a68f707511a");
    protected IWebhookDeliverySigner _signer;
    protected DeliveryClock _clock;
    protected SignedReceiver _receiver;
    protected HttpClient _client;
    protected WebhookMessageIdentity _identity;
    protected WebhookDeliveryResult _result;
    protected AppendedEvent[] _events;

    void Establish()
    {
        _signer = Substitute.For<IWebhookDeliverySigner>();
        _signer.Sign(Arg.Any<EventStoreName>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<ReadOnlyMemory<byte>>(), Arg.Any<CancellationToken>())
            .Returns(call => new WebhookDeliverySignature(call.Arg<string>(), call.Arg<long>().ToString(CultureInfo.InvariantCulture), WebhookEd25519Signing.Sign(call.Arg<string>(), call.Arg<long>(), call.Arg<ReadOnlyMemory<byte>>().Span, _seed), "active-key"));
        _clock = new();
        _receiver = new();
        _client = new(_receiver) { BaseAddress = new("https://example.com/webhook") };
        _httpClientFactory.Create(_target, Arg.Any<string?>()).Returns(_client);
        dynamic content = new ExpandoObject();
        content.Name = "世界 café";
        _events = [AppendedEvent.Empty() with { Content = content, Context = EventContext.Empty with { SequenceNumber = 42 } }];
        _identity = WebhookMessageIdentity.ForEvents(new("webhook", "store", "namespace", "log"), "partition", _events, null);
        _mediator = new(_httpClientFactory, _jsonOptions, logger: _logs, signer: _signer, timeProvider: _clock);
    }

    protected Task<WebhookDeliveryResult> Deliver(WebhookMessageIdentity? identity) => _mediator.Deliver("store", _target, "partition", _events, signingIdentity: identity);
    void Destroy() => _client.Dispose();

    public sealed class SignedReceiver : HttpMessageHandler
    {
        public List<byte[]> Bodies { get; } = [];
        public List<Dictionary<string, string[]>> Headers { get; } = [];
        public List<string> ContentTypes { get; } = [];
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;

        public bool Verifies(int index) => Headers[index].TryGetValue("webhook-signature", out var signatures) &&
            WebhookSignatureVerification.Verify(Headers[index]["webhook-id"].Single(), long.Parse(Headers[index]["webhook-timestamp"].Single(), CultureInfo.InvariantCulture), Bodies[index], signatures.Single(), [], [_publicKey], Headers[index]["Cratis-Key-Id"].Single());

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Bodies.Add(await request.Content!.ReadAsByteArrayAsync(cancellationToken));
            Headers.Add(request.Headers.ToDictionary(header => header.Key, header => header.Value.ToArray(), StringComparer.OrdinalIgnoreCase));
            ContentTypes.Add(request.Content.Headers.ContentType!.ToString());
            var response = new HttpResponseMessage(Status);
            if (Status == HttpStatusCode.Found) response.Headers.Location = new("https://redirect.example.com");
            return response;
        }
    }
}
