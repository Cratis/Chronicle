// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class with_a_short_lived_token : given.a_controlled_oauth_client
{
    AccessTokenInfo _first;

    void Establish()
    {
        _endpoint.Responses.Enqueue(() => Task.FromResult(Discovery()));
        _endpoint.Responses.Enqueue(() => Task.FromResult(Token(10)));
        _endpoint.Responses.Enqueue(() => Task.FromResult(Token(10)));
    }

    async Task Because()
    {
        _first = await _client.AcquireToken(_authorization);
        _clock.Now += TimeSpan.FromSeconds(5);
        await _client.AcquireToken(_authorization);
    }

    [Fact] void should_reserve_half_the_lifetime_as_a_refresh_margin() => _first.ExpiresAt.ShouldEqual(new DateTimeOffset(2026, 1, 1, 0, 0, 5, TimeSpan.Zero));
    [Fact] void should_refresh_at_the_exact_expiry_boundary() => _endpoint.Requests.ShouldEqual(3);
}
