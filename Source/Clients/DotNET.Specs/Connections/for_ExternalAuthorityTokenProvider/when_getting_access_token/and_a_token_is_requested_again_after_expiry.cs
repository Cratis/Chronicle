// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.when_getting_access_token;

public class and_a_token_is_requested_again_after_expiry : given.an_external_authority
{
    async Task Establish()
    {
        _handler.ExpiresIn = 120;
        await _provider.GetAccessToken();
        _time.Advance(TimeSpan.FromSeconds(61));
    }

    async Task Because() => await _provider.GetAccessToken();

    [Fact] void should_discover_only_once() => _handler.DiscoveryRequests.Count().ShouldEqual(1);
    [Fact] void should_post_twice() => _handler.TokenRequests.Count().ShouldEqual(2);
}
