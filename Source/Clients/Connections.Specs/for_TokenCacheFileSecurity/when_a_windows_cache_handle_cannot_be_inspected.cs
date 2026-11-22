// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;

namespace Cratis.Chronicle.Connections.for_TokenCacheFileSecurity;

public class when_a_windows_cache_handle_cannot_be_inspected : Specification
{
    bool _canRead;

    void Because()
    {
        if (OperatingSystem.IsWindows())
        {
            using var handle = new SafeFileHandle(IntPtr.Zero, ownsHandle: false);
            _canRead = TokenCacheFileSecurity.CanRead(handle);
        }
    }

    [Fact]
    void should_treat_the_cache_as_a_miss()
    {
        if (OperatingSystem.IsWindows())
        {
            _canRead.ShouldBeFalse();
        }
    }
}
