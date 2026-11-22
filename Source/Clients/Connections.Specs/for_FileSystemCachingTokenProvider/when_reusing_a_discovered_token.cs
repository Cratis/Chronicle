// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_reusing_a_discovered_token : given.a_persisted_authority_token
{
    string? _result;
    string _persistedAuthority;

    async Task Because()
    {
        _result = await ReadPersistedToken();
        using var cache = JsonDocument.Parse(await File.ReadAllTextAsync(_cacheFile));
        _persistedAuthority = cache.RootElement.GetProperty("Binding").GetProperty("Authority").GetString()!;
    }

    [Fact] void should_verify_the_discovered_endpoint() => _handler.DiscoveryRequests.ShouldEqual(1);
    [Fact] void should_reuse_the_persisted_token() => _result.ShouldEqual("acquired-token");
    [Fact] void should_not_request_another_token() => _handler.TokenRequests.ShouldEqual(0);
    [Fact] void should_persist_the_authority() => _persistedAuthority.ShouldEqual(_authority.AbsoluteUri);
}
