// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation.Webhooks;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_WebhookMediator.when_calling_on_next;

public class and_events_belong_to_an_event_store : given.a_webhook_mediator
{
    CapturingHandler _handler;
    HttpClient _httpClient;
    WebhookTarget _target;

    void Establish()
    {
        _handler = new CapturingHandler();
        _httpClient = new HttpClient(_handler) { BaseAddress = new Uri("https://receiver.example") };
        _target = new WebhookTarget("https://receiver.example", WebhookAuthorization.None, new Dictionary<string, string>());
        _httpClientFactory.Create(Arg.Any<WebhookTarget>(), null).Returns(_httpClient);
    }

    async Task Because() => await _mediator.OnNext("orders", _target, new Key("partition", Properties.ArrayIndexers.NoIndexers), [AppendedEvent.Empty() with { Context = EventContext.Empty with { EventStore = "untrusted-payload-store" } }]);

    [Fact] void should_tag_the_delivery_request() => _handler.EventStore.ShouldEqual((EventStoreName)"orders");

    void Destroy() => _httpClient.Dispose();

    sealed class CapturingHandler : HttpMessageHandler
    {
        readonly List<HttpResponseMessage> _responses = [];

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var response in _responses) response.Dispose();
            }
            base.Dispose(disposing);
        }

        public EventStoreName? EventStore { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Options.TryGetValue(WebhookRequestOptions.EventStore, out var store);
            EventStore = store;
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            _responses.Add(response);
            return Task.FromResult(response);
        }
    }
}
