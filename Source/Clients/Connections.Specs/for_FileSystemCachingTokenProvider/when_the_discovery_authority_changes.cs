// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_the_discovery_authority_changes : given.a_persisted_authority_token
{
    void Establish() => _authority = new Uri("https://other.example/realm");

    async Task Because() => await ReadPersistedToken();

    [Fact] void should_discover_the_new_authority() => _handler.DiscoveryRequests.ShouldEqual(1);
    [Fact] void should_not_reuse_a_token_from_another_authority() => _handler.TokenRequests.ShouldEqual(1);
}
