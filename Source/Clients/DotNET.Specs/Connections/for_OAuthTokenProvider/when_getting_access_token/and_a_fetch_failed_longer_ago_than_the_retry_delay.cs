// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_getting_access_token;

public class and_a_fetch_failed_longer_ago_than_the_retry_delay : given.a_token_provider_with_controllable_endpoint
{
    string _token;

    async Task Establish()
    {
        _endpoint.Fail = true;
        await Catch.Exception(() => _provider.GetAccessToken());
        _endpoint.Fail = false;
        _time.Advance(TimeSpan.FromSeconds(6));
    }

    async Task Because() => _token = (await _provider.GetAccessToken())!;

    [Fact] void should_fetch_again() => _endpoint.Requests.ShouldEqual(2);
    [Fact] void should_return_the_fetched_token() => _token.ShouldEqual(_endpoint.Token);
}
