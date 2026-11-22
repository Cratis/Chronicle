// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.AccessControl;
using System.Security.Principal;

namespace Cratis.Chronicle.Connections.for_TokenCacheFileSecurity;

public class when_a_windows_cache_file_grants_foreign_write_access : given.a_windows_cache_file
{
    void Establish()
    {
        if (OperatingSystem.IsWindows())
        {
            var file = new FileInfo(_path);
            var security = file.GetAccessControl();
            security.AddAccessRule(new FileSystemAccessRule(
                new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.Write, AccessControlType.Allow));
            file.SetAccessControl(security);
        }
    }

    void Because()
    {
        if (OperatingSystem.IsWindows())
        {
            using var handle = File.OpenHandle(_path);
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
