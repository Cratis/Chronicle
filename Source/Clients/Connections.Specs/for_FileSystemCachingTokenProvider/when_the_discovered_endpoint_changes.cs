// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_the_discovered_endpoint_changes : given.a_persisted_authority_token
{
    void Establish() => _handler.TokenEndpoint = "https://other.example/oauth/token";

    async Task Because() => await ReadPersistedToken();

    [Fact] void should_verify_the_discovered_endpoint() => _handler.DiscoveryRequests.ShouldEqual(1);
    [Fact] void should_acquire_a_token_for_the_new_endpoint() => _handler.TokenRequests.ShouldEqual(1);
}
