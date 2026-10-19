// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class after_the_refresh_margin : given.a_cached_oauth_client
{
    AccessTokenInfo _second;

    async Task Because() {
        await _client.AcquireToken(_authorization);
        _clock.Advance(TimeSpan.FromSeconds(3540));
        _second = await _client.AcquireToken(_authorization);
    }

    [Fact] void should_fetch_again() => _endpoint.TokenRequests.ShouldEqual(2);
    [Fact] void should_return_new_token() => _second.AccessToken.ShouldEqual("token-2");
}
