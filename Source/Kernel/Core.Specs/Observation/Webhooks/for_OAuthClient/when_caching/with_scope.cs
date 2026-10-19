// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class with_scope : given.a_cached_oauth_client
{
    void Establish() => _authorization = _authorization with { Scope = "api://resource/.default" };
    async Task Because() => await _client.AcquireToken(_authorization);

    [Fact] void should_send_scope() => _endpoint.Form.ShouldContain("scope=api%3A%2F%2Fresource%2F.default");
}
