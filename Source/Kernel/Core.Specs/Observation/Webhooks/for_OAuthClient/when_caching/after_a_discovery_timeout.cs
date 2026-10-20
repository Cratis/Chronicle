// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class after_a_discovery_timeout : given.a_cached_oauth_client
{
    Exception _error;
    AccessTokenInfo _token;

    async Task Because()
    {
        _endpoint.DiscoveryTimeouts = 1;
        _error = await Catch.Exception(() => _client.AcquireToken(_authorization));
        _token = await _client.AcquireToken(_authorization);
    }

    [Fact] void should_fail_the_timed_out_attempt() => _error.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] void should_discover_again() => _endpoint.DiscoveryRequests.ShouldEqual(2);
    [Fact] void should_acquire_a_token_after_recovering() => _token.AccessToken.ShouldEqual("token-1");
}
