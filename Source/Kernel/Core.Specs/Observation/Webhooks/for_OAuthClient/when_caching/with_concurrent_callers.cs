// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class with_concurrent_callers : given.a_cached_oauth_client
{
    AccessTokenInfo[] _results;
    void Establish() => _endpoint.Block = true;
    async Task Because() {
        var first = _client.AcquireToken(_authorization);
        var second = _client.AcquireToken(_authorization);
        _release.SetResult();
        _results = await Task.WhenAll(first, second);
    }

    [Fact] void should_fetch_once() => _endpoint.TokenRequests.ShouldEqual(1);
    [Fact] void should_share_token() => _results[1].ShouldEqual(_results[0]);
}
