// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_the_cache_is_malformed : given.a_caching_provider
{
    string? _result;

    async Task Establish()
    {
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("first-token", "fresh-token");
        await _provider.GetAccessToken();
        await File.WriteAllTextAsync(_cacheFile, "{truncated");
    }

    async Task Because() => _result = await _provider.GetAccessToken();

    [Fact] void should_reacquire() => _result.ShouldEqual("fresh-token");
    [Fact] void should_replace_the_malformed_entry() => CacheProperty("AccessToken").ShouldEqual("fresh-token");
}
