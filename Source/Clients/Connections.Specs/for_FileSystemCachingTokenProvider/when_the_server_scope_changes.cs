// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_the_server_scope_changes : given.a_caching_provider
{
    string? _result;

    async Task Establish()
    {
        _provider.Dispose();
        _provider = new(_inner, _cacheFile, null, "server-one|client-one");
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("old-server-token", "fresh-token");
        await _provider.GetAccessToken();
    }

    async Task Because()
    {
        using var other = new FileSystemCachingTokenProvider(_inner, _cacheFile, null, "server-two|client-one");
        _result = await other.GetAccessToken();
        await other.GetAccessToken();
    }

    [Fact] void should_not_reuse_a_different_servers_token() => _result.ShouldEqual("fresh-token");
    [Fact] void should_overwrite_the_mismatched_entry() => CacheProperty("AccessToken").ShouldEqual("fresh-token");
    [Fact] void should_reuse_the_replacement() => _inner.Received(2).GetAccessToken(Arg.Any<CancellationToken>());
}
