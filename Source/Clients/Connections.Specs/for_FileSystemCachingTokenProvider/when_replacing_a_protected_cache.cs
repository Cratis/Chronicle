// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.AccessControl;
using System.Security.Principal;

namespace Cratis.Chronicle.Connections.for_FileSystemCachingTokenProvider;

public class when_replacing_a_protected_cache : given.a_caching_provider
{
    async Task Establish()
    {
        _inner.GetAccessToken(Arg.Any<CancellationToken>()).Returns("old-token");
        _inner.Refresh(Arg.Any<CancellationToken>()).Returns("refreshed-token");
        await _provider.GetAccessToken();
    }

    async Task Because() => await _provider.Refresh();

    [Fact]
    void should_preserve_owner_only_permissions_on_replacement()
    {
        if (OperatingSystem.IsWindows())
        {
            var security = new FileInfo(_cacheFile).GetAccessControl();
            security.AreAccessRulesProtected.ShouldBeTrue();
            var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();
            rules.Length.ShouldEqual(1);
            rules[0].IdentityReference.ShouldEqual(WindowsIdentity.GetCurrent().User);
            rules[0].FileSystemRights.ShouldEqual(FileSystemRights.FullControl);
        }
        else
        {
            File.GetUnixFileMode(_cacheFile).ShouldEqual(UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }
    }

    [Fact]
    void should_protect_the_windows_directory()
    {
        if (OperatingSystem.IsWindows())
        {
            var security = new DirectoryInfo(CacheDirectory).GetAccessControl();
            security.AreAccessRulesProtected.ShouldBeTrue();
            var rules = security.GetAccessRules(true, true, typeof(SecurityIdentifier)).Cast<FileSystemAccessRule>().ToArray();
            rules.Length.ShouldEqual(1);
            rules[0].IdentityReference.ShouldEqual(WindowsIdentity.GetCurrent().User);
            rules[0].FileSystemRights.ShouldEqual(FileSystemRights.FullControl);
            rules[0].InheritanceFlags.ShouldEqual(InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit);
        }
    }
}
