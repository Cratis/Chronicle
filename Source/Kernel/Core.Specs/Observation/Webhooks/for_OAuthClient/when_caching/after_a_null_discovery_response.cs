// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class after_a_null_discovery_response : given.a_controlled_oauth_client
{
    AccessTokenInfo _first;
    AccessTokenInfo _recovered;

    void Establish()
    {
        _endpoint.Responses.Enqueue(() => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("null") }));
        _endpoint.Responses.Enqueue(() => Task.FromResult(Discovery()));
        _endpoint.Responses.Enqueue(() => Task.FromResult(Token()));
    }

    async Task Because()
    {
        _first = await _client.AcquireToken(_authorization);
        _recovered = await _client.AcquireToken(_authorization);
    }

    [Fact] void should_return_an_empty_token_for_null_configuration() => _first.ShouldEqual(AccessTokenInfo.Empty);
    [Fact] void should_not_cache_the_null_configuration() => _endpoint.Requests.ShouldEqual(3);
    [Fact] void should_acquire_a_token_after_recovering() => _recovered.AccessToken.ShouldEqual("token");
}
