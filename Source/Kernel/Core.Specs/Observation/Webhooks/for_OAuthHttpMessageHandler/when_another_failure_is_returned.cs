// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthHttpMessageHandler;

public class when_another_failure_is_returned : for_OAuthClient.given.a_cached_oauth_client
{
    HttpStatusCode _status;

    void Establish() => _endpoint.Responses = new([HttpStatusCode.Forbidden]);

    async Task Because()
    {
        using var client = OAuthHttpMessageHandler.CreateClient(_factory.CreateClient(), _authorization, _client);
        using var request = new HttpRequestMessage(HttpMethod.Put, "https://target.example/resource") { Content = new StringContent("payload") };
        request.Headers.Add("X-Test", "value");
        using var response = await client.SendAsync(request);
        _status = response.StatusCode;
    }

    [Fact] void should_return_the_response() => _status.ShouldEqual(HttpStatusCode.Forbidden);
    [Fact] void should_send_expected_number_of_times() => _endpoint.SentTokens.Count.ShouldEqual(1);
    [Fact] void should_preserve_payload() => _endpoint.Bodies.TrueForAll(_ => _ == "payload").ShouldBeTrue();
    [Fact] void should_preserve_headers() => _endpoint.Headers.TrueForAll(_ => _).ShouldBeTrue();
    [Fact] void should_preserve_method() => _endpoint.Methods.TrueForAll(_ => _ == HttpMethod.Put).ShouldBeTrue();
    [Fact] void should_fetch_expected_number_of_tokens() => _endpoint.TokenRequests.ShouldEqual(1);
}
