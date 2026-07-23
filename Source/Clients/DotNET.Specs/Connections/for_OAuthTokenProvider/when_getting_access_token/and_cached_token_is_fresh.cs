// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_getting_access_token;

public class and_cached_token_is_fresh : given.a_token_provider_with_controllable_endpoint
{
    string _token;

    async Task Establish() => await _provider.GetAccessToken();

    async Task Because() => _token = (await _provider.GetAccessToken())!;

    [Fact] void should_return_the_cached_token() => _token.ShouldEqual(_endpoint.Token);
    [Fact] void should_not_fetch_again() => _endpoint.Requests.ShouldEqual(1);
}
