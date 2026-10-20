// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Security;
using Cratis.Chronicle.Observation.Webhooks.Validation;

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthHttpMessageHandler;

public class when_retrying_with_an_event_store : Specification
{
    IOAuthClient _tokens;
    OAuthAuthorization _authorization;
    Receiver _receiver;
    HttpStatusCode _status;

    void Establish()
    {
        _tokens = Substitute.For<IOAuthClient>();
        _authorization = new("https://authority.example", "client", "secret") { Scope = "api://target/.default" };
        _tokens.AcquireToken(_authorization, (EventStoreName)"orders", Arg.Any<CancellationToken>())
            .Returns(new AccessTokenInfo("token", DateTimeOffset.UtcNow.AddHours(1)));
        _receiver = new();
    }

    async Task Because()
    {
        using var innerClient = new HttpClient(_receiver);
        using var client = OAuthHttpMessageHandler.CreateClient(innerClient, _authorization, _tokens);
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://receiver.example");
        request.Options.Set(WebhookRequestOptions.EventStore, (EventStoreName)"orders");
        using var response = await client.SendAsync(request);
        _status = response.StatusCode;
    }

    [Fact] void should_succeed_after_one_retry() => _status.ShouldEqual(HttpStatusCode.OK);
    [Fact] async Task should_use_the_store_and_scope_for_both_token_requests() =>
        await _tokens.Received(2).AcquireToken(_authorization, (EventStoreName)"orders", Arg.Any<CancellationToken>());
    [Fact] void should_send_twice() => _receiver.Stores.Count.ShouldEqual(2);
    [Fact] void should_preserve_the_store_on_the_initial_request() => _receiver.Stores[0].ShouldEqual((EventStoreName)"orders");
    [Fact] void should_preserve_the_store_on_the_retry() => _receiver.Stores[1].ShouldEqual((EventStoreName)"orders");

    sealed class Receiver : HttpMessageHandler
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

        public List<EventStoreName?> Stores { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Options.TryGetValue(WebhookRequestOptions.EventStore, out var store);
            Stores.Add(store);
            var response = new HttpResponseMessage(Stores.Count == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK);
            _responses.Add(response);
            return Task.FromResult(response);
        }
    }
}
