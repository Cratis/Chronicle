// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class with_repeated_delivery_validation : given.a_controlled_oauth_client
{
    void Establish()
    {
        _endpoint.Responses.Enqueue(() => Task.FromResult(Discovery()));
        _endpoint.Responses.Enqueue(() => Task.FromResult(Token()));
    }

    async Task Because()
    {
        await _client.AcquireToken(_authorization);
        await _client.AcquireToken(_authorization);
    }

    [Fact]
    void should_validate_before_each_request_and_before_cache_reuse() => _calls.ShouldEqual(
        "validate:https://authority.example/.well-known/openid-configuration",
        "send:https://authority.example/.well-known/openid-configuration",
        "validate:https://authority.example/token",
        "send:https://authority.example/token",
        "validate:https://authority.example/.well-known/openid-configuration",
        "validate:https://authority.example/token");
}
