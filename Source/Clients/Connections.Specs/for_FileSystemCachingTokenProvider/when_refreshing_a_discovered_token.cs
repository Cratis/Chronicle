// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_refreshing_a_discovered_token : given.a_persisted_authority_token
{
    string? _result;
    string _persistedAuthority;

    async Task Establish()
    {
        await ReadPersistedToken();
        _handler.AccessToken = "refreshed-token";
    }

    async Task Because()
    {
        _result = await _provider.Refresh();
        using var cache = JsonDocument.Parse(await File.ReadAllTextAsync(_cacheFile));
        _persistedAuthority = cache.RootElement.GetProperty("Binding").GetProperty("Authority").GetString()!;
    }

    [Fact] void should_return_the_refreshed_token() => _result.ShouldEqual("refreshed-token");
    [Fact] void should_request_one_new_token() => _handler.TokenRequests.ShouldEqual(1);
    [Fact] void should_persist_the_refreshed_token() => CacheProperty("AccessToken").ShouldEqual("refreshed-token");
    [Fact] void should_preserve_the_authority_binding() => _persistedAuthority.ShouldEqual(_authority.AbsoluteUri);
}
