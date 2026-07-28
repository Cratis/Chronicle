// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_getting_access_token;

public class and_fetching_new_token_fails_after_expiry : given.a_token_provider_with_controllable_endpoint
{
    Exception _exception;

    async Task Establish()
    {
        await _provider.GetAccessToken();
        _time.Advance(TimeSpan.FromSeconds(3601));
        _endpoint.Fail = true;
    }

    async Task Because() => _exception = await Catch.Exception(() => _provider.GetAccessToken());

    [Fact] void should_fail() => _exception.ShouldNotBeNull();
}
