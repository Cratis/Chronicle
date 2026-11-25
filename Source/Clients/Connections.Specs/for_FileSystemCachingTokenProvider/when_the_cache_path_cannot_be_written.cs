// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_the_cache_path_cannot_be_written : given.a_caching_provider
{
    string? _result;

    void Establish()
    {
        Directory.CreateDirectory(_cacheFile);
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("fresh-token");
    }

    async Task Because() => _result = await _provider.GetAccessToken();

    [Fact] void should_return_the_token_despite_the_write_failure() => _result.ShouldEqual("fresh-token");
    [Fact] void should_clean_up_the_failed_replacement() => Directory.GetFiles(CacheDirectory).Length.ShouldEqual(0);
}
