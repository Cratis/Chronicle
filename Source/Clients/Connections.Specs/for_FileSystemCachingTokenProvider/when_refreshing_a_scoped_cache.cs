// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_refreshing_a_scoped_cache : given.a_caching_provider
{
    string? _result;

    async Task Establish()
    {
        _provider.Dispose();
        _provider = new(_inner, _cacheFile, null, "server-one|client-one");
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("old-token");
        _inner.Refresh(Arg.Any<CancellationToken>()).Returns("refreshed-token");
        await _provider.GetAccessToken();
    }

    async Task Because()
    {
        await _provider.Refresh();
        _result = await _provider.GetAccessToken();
    }

    [Fact] void should_reuse_the_refreshed_token() => _result.ShouldEqual("refreshed-token");
    [Fact] void should_preserve_the_scope() => CacheProperty("ScopeHash").Length.ShouldEqual(64);
    [Fact] void should_refresh_once() => _inner.Received(1).Refresh(Arg.Any<CancellationToken>());
}
