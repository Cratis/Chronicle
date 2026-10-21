// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class after_a_failed_fetch : given.a_cached_oauth_client
{
    async Task Because()
    {
        _endpoint.Fail = true;
        try
        {
            await _client.AcquireToken(_authorization);
        }
        catch (HttpRequestException)
        {
        }

        _endpoint.Fail = false;
        await _client.AcquireToken(_authorization);
    }

    [Fact] void should_not_cache_failure() => _endpoint.TokenRequests.ShouldEqual(2);
}
