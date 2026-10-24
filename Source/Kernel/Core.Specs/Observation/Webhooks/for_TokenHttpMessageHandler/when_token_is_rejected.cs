// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Observation.Webhooks.for_TokenHttpMessageHandler;

public class when_token_is_rejected : for_OAuthClient.given.a_cached_oauth_client
{
    HttpStatusCode _status;

    void Establish() => _endpoint.Responses = new([HttpStatusCode.Unauthorized, HttpStatusCode.OK]);

    async Task Because()
    {
        using var client = TokenHttpMessageHandler.CreateClient(_factory.CreateClient(), (store, cancellation) => _client.AcquireToken(_authorization, store, cancellation), token => _client.Invalidate(_authorization, token));
        using var request = new HttpRequestMessage(HttpMethod.Put, "https://target.example/resource") { Content = new StringContent("payload") };
        request.Headers.Add("X-Test", "value");
        using var response = await client.SendAsync(request);
        _status = response.StatusCode;
    }

    [Fact] void should_retry_once() => _endpoint.SentTokens.Count.ShouldEqual(2);
    [Fact] void should_use_a_fresh_token() => _endpoint.SentTokens[1].ShouldEqual("token-2");
    [Fact] void should_return_the_response() => _status.ShouldEqual(HttpStatusCode.OK);
    [Fact] void should_preserve_the_body() => _endpoint.Bodies.TrueForAll(_ => _ == "payload").ShouldBeTrue();
    [Fact] void should_preserve_the_headers() => _endpoint.Headers.TrueForAll(_ => _).ShouldBeTrue();
}
