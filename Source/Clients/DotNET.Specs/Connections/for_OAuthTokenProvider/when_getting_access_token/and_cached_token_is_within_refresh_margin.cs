// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_getting_access_token;

public class and_cached_token_is_within_refresh_margin : given.a_token_provider_with_controllable_endpoint
{
    string _token;

    async Task Establish()
    {
        _endpoint.Token = "first-token";
        await _provider.GetAccessToken();
        _endpoint.Token = "second-token";
        _time.Advance(TimeSpan.FromSeconds(3570));
    }

    async Task Because() => _token = (await _provider.GetAccessToken())!;

    [Fact] void should_fetch_a_new_token() => _endpoint.Requests.ShouldEqual(2);
    [Fact] void should_return_the_new_token() => _token.ShouldEqual("second-token");
}
