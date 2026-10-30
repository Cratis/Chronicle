// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Observation.Webhooks.for_AzureIdentityManagedIdentityTokens.when_acquiring;

public class with_a_rejected_token : given.a_token_provider
{
    async Task Establish()
    {
        _first = await _provider.AcquireToken(_authorization, CancellationToken.None);
        _provider.Invalidate(_authorization, _first);
    }

    async Task Because() => _result = await _provider.AcquireToken(_authorization, CancellationToken.None);

    [Fact] void should_fetch_a_new_token() => _result.AccessToken.ShouldEqual("token-2");
}
