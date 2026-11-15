// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_OAuthTokenProvider.when_requesting_a_token;

public class and_the_external_token_expires : given.a_token_provider_with_an_external_endpoint
{
    async Task Establish()
    {
        _handler.ExpiresIn = 120;
        await _provider.GetAccessToken();
        _time.Advance(TimeSpan.FromSeconds(61));
    }

    async Task Because() => await _provider.GetAccessToken();

    [Fact] void should_post_twice() => _handler.Requests.Count.ShouldEqual(2);
    [Fact] void should_post_both_times_to_the_external_endpoint() => _handler.Requests.TrueForAll(_ => _.Uri == _endpoint).ShouldBeTrue();
}
