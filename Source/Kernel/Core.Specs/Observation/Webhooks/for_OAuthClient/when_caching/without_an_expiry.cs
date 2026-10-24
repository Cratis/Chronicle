// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class without_an_expiry : given.a_cached_oauth_client
{
    void Establish() => _lifetime = 0;

    async Task Because()
    {
        await _client.AcquireToken(_authorization);
        await _client.AcquireToken(_authorization);
    }

    [Fact] void should_not_cache() => _endpoint.TokenRequests.ShouldEqual(2);
}
