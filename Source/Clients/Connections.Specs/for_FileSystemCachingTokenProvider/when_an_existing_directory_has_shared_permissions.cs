// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_an_existing_directory_has_shared_permissions : given.a_caching_provider
{
    void Establish()
    {
        Directory.CreateDirectory(CacheDirectory);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                CacheDirectory,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }

        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("fresh-token");
    }

    async Task Because() => await _provider.GetAccessToken();

    [Fact]
    void should_restrict_the_existing_directory_to_its_owner()
    {
        if (!OperatingSystem.IsWindows())
        {
            File.GetUnixFileMode(CacheDirectory).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }
}
