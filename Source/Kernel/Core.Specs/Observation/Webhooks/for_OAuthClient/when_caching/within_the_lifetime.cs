// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class within_the_lifetime : given.a_cached_oauth_client
{
    AccessTokenInfo _first;
    AccessTokenInfo _second;

    async Task Because() {
        _first = await _client.AcquireToken(_authorization);
        _clock.Advance(TimeSpan.FromSeconds(3500));
        _second = await _client.AcquireToken(_authorization);
    }

    [Fact] void should_fetch_once() => _endpoint.TokenRequests.ShouldEqual(1);
    [Fact] void should_reuse_token() => _second.ShouldEqual(_first);
}
