// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_getting_access_token;

public class and_fetching_new_token_fails_while_cached_is_valid : given.a_token_provider_with_controllable_endpoint
{
    string _token;

    async Task Establish()
    {
        _endpoint.Token = "cached-token";
        await _provider.GetAccessToken();
        _time.Advance(TimeSpan.FromSeconds(3570));
        _endpoint.Fail = true;
    }

    async Task Because() => _token = (await _provider.GetAccessToken())!;

    [Fact] void should_attempt_a_fetch() => _endpoint.Requests.ShouldEqual(2);
    [Fact] void should_serve_the_cached_token() => _token.ShouldEqual("cached-token");
}
