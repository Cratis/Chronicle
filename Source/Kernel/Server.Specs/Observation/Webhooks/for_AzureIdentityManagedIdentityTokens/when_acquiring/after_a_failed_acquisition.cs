// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Observation.Webhooks.for_AzureIdentityManagedIdentityTokens.when_acquiring;

public class after_a_failed_acquisition : given.a_token_provider
{
    async Task Establish()
    {
        _credential.Fail = true;
        await Catch.Exception(async () => await _provider.AcquireToken(_authorization, CancellationToken.None));
        _credential.Fail = false;
    }

    async Task Because() => _result = await _provider.AcquireToken(_authorization, CancellationToken.None);

    [Fact] void should_not_cache_the_failure() => _result.AccessToken.ShouldEqual("token-2");
}
