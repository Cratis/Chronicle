// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_refreshing;

public class and_a_fetch_recently_failed : given.a_token_provider_with_controllable_endpoint
{
    string _token;

    async Task Establish()
    {
        _endpoint.Fail = true;
        await Catch.Exception(() => _provider.GetAccessToken());
        _endpoint.Fail = false;
    }

    async Task Because() => _token = (await _provider.Refresh())!;

    [Fact] void should_bypass_the_throttle_and_fetch() => _endpoint.Requests.ShouldEqual(2);
    [Fact] void should_return_the_fetched_token() => _token.ShouldEqual(_endpoint.Token);
}
