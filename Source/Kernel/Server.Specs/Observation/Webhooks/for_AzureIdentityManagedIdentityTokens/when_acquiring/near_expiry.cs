// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Observation.Webhooks.for_AzureIdentityManagedIdentityTokens.when_acquiring;

public class near_expiry : given.a_token_provider
{
    async Task Establish()
    {
        _first = await _provider.AcquireToken(_authorization, CancellationToken.None);
        _clock.Now = _first.ExpiresAt.AddSeconds(-30);
    }

    async Task Because() => _result = await _provider.AcquireToken(_authorization, CancellationToken.None);

    [Fact] void should_refresh_the_token() => _result.AccessToken.ShouldEqual("token-2");
    [Fact] void should_reuse_the_credential() => _factory.Received(1).Create(_authorization.ClientId);
}
