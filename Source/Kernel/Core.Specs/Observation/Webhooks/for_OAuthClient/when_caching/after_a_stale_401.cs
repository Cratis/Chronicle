// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class after_a_stale_401 : given.a_cached_oauth_client
{
    AccessTokenInfo _replacement;
    AccessTokenInfo _current;

    async Task Because()
    {
        var first = await _client.AcquireToken(_authorization);
        _client.Invalidate(_authorization, first);
        _replacement = await _client.AcquireToken(_authorization);
        _client.Invalidate(_authorization, first);
        _current = await _client.AcquireToken(_authorization);
    }

    [Fact] void should_not_invalidate_the_replacement() => _current.ShouldEqual(_replacement);
    [Fact] void should_fetch_only_twice() => _endpoint.TokenRequests.ShouldEqual(2);
}
