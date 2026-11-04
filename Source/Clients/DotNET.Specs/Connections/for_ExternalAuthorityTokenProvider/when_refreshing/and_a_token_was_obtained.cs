// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_ExternalAuthorityTokenProvider.when_refreshing;

public class and_a_token_was_obtained : given.an_external_authority
{
    async Task Establish() => await _provider.GetAccessToken();

    async Task Because() => await _provider.Refresh();

    [Fact] void should_post_again() => _handler.TokenRequests.Count().ShouldEqual(2);
    [Fact] void should_not_discover_again() => _handler.DiscoveryRequests.Count().ShouldEqual(1);
}
