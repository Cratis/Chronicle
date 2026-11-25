// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_the_original_constructor_reads_a_scoped_entry : given.a_caching_provider
{
    string? _result;

    async Task Establish()
    {
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("cached-token", "fresh-token");
        using var scoped = new FileSystemCachingTokenProvider(_inner, _cacheFile, null, "server-one|client-one");
        await scoped.GetAccessToken();
    }

    async Task Because() => _result = await _provider.GetAccessToken();

    [Fact] void should_preserve_unscoped_reuse() => _result.ShouldEqual("cached-token");
    [Fact] void should_not_acquire_again() => _inner.Received(1).GetAccessToken(Arg.Any<CancellationToken>());
}
