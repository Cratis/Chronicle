// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.AccessControl;
using System.Security.Principal;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_creating_a_protected_temporary_file : given.a_caching_provider
{
    void Establish() => TokenCacheFileSecurity.CreateDirectory(CacheDirectory);

    void Because()
    {
        using var stream = TokenCacheFileSecurity.CreateFile(_cacheFile);
        stream.WriteByte(1);
    }

    [Fact]
    void should_be_private_from_creation()
    {
        if (OperatingSystem.IsWindows())
        {
            var security = new FileInfo(_cacheFile).GetAccessControl();
            security.AreAccessRulesProtected.ShouldBeTrue();
            var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();
            rules.Length.ShouldEqual(1);
            rules[0].IdentityReference.ShouldEqual(WindowsIdentity.GetCurrent().User);
            rules[0].AccessControlType.ShouldEqual(AccessControlType.Allow);
            rules[0].FileSystemRights.ShouldEqual(FileSystemRights.FullControl);
        }
        else
        {
            File.GetUnixFileMode(_cacheFile).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }
}
