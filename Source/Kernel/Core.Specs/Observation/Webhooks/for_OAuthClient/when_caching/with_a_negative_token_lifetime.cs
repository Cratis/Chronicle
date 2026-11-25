// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class with_a_negative_token_lifetime : given.a_controlled_oauth_client
{
    AccessTokenInfo _first;

    void Establish()
    {
        _endpoint.Responses.Enqueue(() => Task.FromResult(Discovery()));
        _endpoint.Responses.Enqueue(() => Task.FromResult(Token(-10)));
        _endpoint.Responses.Enqueue(() => Task.FromResult(Token()));
    }

    async Task Because()
    {
        _first = await _client.AcquireToken(_authorization);
        await _client.AcquireToken(_authorization);
    }

    [Fact] void should_clamp_expiry_to_the_current_time() => _first.ExpiresAt.ShouldEqual(_clock.Now);
    [Fact] void should_not_reuse_the_expired_token() => _endpoint.Requests.ShouldEqual(3);
}
