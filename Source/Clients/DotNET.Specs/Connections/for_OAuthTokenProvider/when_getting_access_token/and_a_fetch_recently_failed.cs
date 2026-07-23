// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_getting_access_token;

public class and_a_fetch_recently_failed : given.a_token_provider_with_controllable_endpoint
{
    string? _token;

    async Task Establish()
    {
        _endpoint.Fail = true;
        await Catch.Exception(() => _provider.GetAccessToken());
    }

    async Task Because() => _token = await _provider.GetAccessToken();

    [Fact] void should_not_return_a_token() => _token.ShouldBeNull();
    [Fact] void should_not_attempt_another_fetch() => _endpoint.Requests.ShouldEqual(1);
}
