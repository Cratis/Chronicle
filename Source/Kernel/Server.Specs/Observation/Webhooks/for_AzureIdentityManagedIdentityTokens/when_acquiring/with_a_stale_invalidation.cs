// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server.Observation.Webhooks.for_AzureIdentityManagedIdentityTokens.when_acquiring;

public class with_a_stale_invalidation : given.a_token_provider
{
    async Task Establish()
    {
        _first = await _provider.AcquireToken(_authorization, CancellationToken.None);
        _provider.Invalidate(_authorization, _first);
        _result = await _provider.AcquireToken(_authorization, CancellationToken.None);
    }

    async Task Because()
    {
        _provider.Invalidate(_authorization, _first);
        _result = await _provider.AcquireToken(_authorization, CancellationToken.None);
    }

    [Fact] void should_keep_the_newer_token() => _credential.Requests.ShouldEqual(2);
}
