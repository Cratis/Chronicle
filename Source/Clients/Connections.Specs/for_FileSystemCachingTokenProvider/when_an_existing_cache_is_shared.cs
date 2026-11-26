// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_an_existing_cache_is_shared : given.a_caching_provider
{
    string? _result;

    async Task Establish()
    {
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("first-token", "fresh-token");
        await _provider.GetAccessToken();
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(_cacheFile, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead);
        }
    }

    async Task Because() => _result = await _provider.GetAccessToken();

    [Fact]
    void should_not_trust_a_shared_cache()
    {
        if (!OperatingSystem.IsWindows())
        {
            _result.ShouldEqual("fresh-token");
        }
    }

    [Fact]
    void should_protect_the_replacement()
    {
        if (!OperatingSystem.IsWindows())
        {
            File.GetUnixFileMode(_cacheFile).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
