// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class after_a_faulted_discovery : given.a_controlled_oauth_client
{
    Exception _error;
    AccessTokenInfo _token;

    void Establish()
    {
        _endpoint.Responses.Enqueue(() => Task.FromException<HttpResponseMessage>(new HttpRequestException("Discovery failed")));
        _endpoint.Responses.Enqueue(() => Task.FromResult(Discovery()));
        _endpoint.Responses.Enqueue(() => Task.FromResult(Token()));
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _client.AcquireToken(_authorization));
        _token = await _client.AcquireToken(_authorization);
    }

    [Fact] void should_preserve_the_discovery_failure() => _error.ShouldBeOfExactType<HttpRequestException>();
    [Fact] void should_retry_discovery_before_fetching_the_token() => _endpoint.Requests.ShouldEqual(3);
    [Fact] void should_acquire_a_token_after_recovering() => _token.AccessToken.ShouldEqual("token");
}
