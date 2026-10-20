// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Webhooks.for_OAuthClient.when_caching;

public class without_scope : given.a_cached_oauth_client
{
    async Task Because() => await _client.AcquireToken(_authorization);

    [Fact] void should_omit_scope() => _endpoint.Form.Contains("scope=").ShouldBeFalse();
}
