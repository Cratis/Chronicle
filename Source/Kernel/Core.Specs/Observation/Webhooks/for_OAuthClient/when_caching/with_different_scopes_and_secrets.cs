// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class with_different_scopes_and_secrets : given.a_cached_oauth_client
{
    async Task Because()
    {
        await _client.AcquireToken(_authorization);
        await _client.AcquireToken(_authorization with { Scope = "other" });
        await _client.AcquireToken(_authorization with { ClientSecret = "rotated" });
        await _client.AcquireToken(_authorization with { ClientId = "other-client" });
    }

    [Fact] void should_isolate_tokens() => _endpoint.TokenRequests.ShouldEqual(4);
}
