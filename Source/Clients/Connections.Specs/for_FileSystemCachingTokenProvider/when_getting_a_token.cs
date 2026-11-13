// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_getting_a_token : given.a_caching_provider
{
    string _first;
    string _second;

    void Establish() => _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("opaque-token");

    async Task Because()
    {
        _first = await _provider.GetAccessToken();
        _second = await _provider.GetAccessToken();
    }

    [Fact] void should_return_the_token() => _first.ShouldEqual("opaque-token");
    [Fact] void should_return_the_cached_token_the_second_time() => _second.ShouldEqual("opaque-token");
    [Fact] void should_only_acquire_the_token_once() => _inner.Received(1).GetAccessToken(Arg.Any<CancellationToken>());
    [Fact] void should_leave_no_temporary_files_behind() => Directory.GetFiles(Path.GetDirectoryName(_cacheFile)!).ShouldEqual([_cacheFile]);

    [Fact]
    void should_restrict_the_cache_file_to_the_current_user()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        File.GetUnixFileMode(_cacheFile).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    [Fact]
    void should_restrict_the_cache_directory_to_the_current_user()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        File.GetUnixFileMode(Path.GetDirectoryName(_cacheFile)!).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
    }
}
