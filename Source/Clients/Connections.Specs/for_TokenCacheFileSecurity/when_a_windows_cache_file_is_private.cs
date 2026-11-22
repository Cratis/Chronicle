// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_TokenCacheFileSecurity;

public class when_a_windows_cache_file_is_private : given.a_windows_cache_file
{
    void Because()
    {
        if (OperatingSystem.IsWindows())
        {
            using var handle = File.OpenHandle(_path);
            _canRead = TokenCacheFileSecurity.CanRead(handle);
        }
    }

    [Fact]
    void should_trust_the_cache()
    {
        if (OperatingSystem.IsWindows())
        {
            _canRead.ShouldBeTrue();
        }
    }
}
