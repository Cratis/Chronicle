// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.AccessControl;
using System.Security.Principal;

namespace Cratis.Chronicle.Connections.for_TokenCacheFileSecurity;

public class when_round_tripping_a_windows_cache_file : given.a_windows_cache_file
{
    const string CacheContents = "cached token contents";
    string _currentUser;
    string? _fileOwner;
    string? _directoryOwner;
    string? _contents;

    void Because()
    {
        if (OperatingSystem.IsWindows())
        {
            using var identity = WindowsIdentity.GetCurrent();
            _currentUser = identity.User!.Value;
            var path = $"{_path}.roundtrip";
            using (var writer = new StreamWriter(TokenCacheFileSecurity.CreateFile(path)))
            {
                writer.Write(CacheContents);
            }

            using var stream = File.OpenRead(path);
            _fileOwner = stream.GetAccessControl().GetOwner(typeof(SecurityIdentifier))?.Value;
            _directoryOwner = new DirectoryInfo(Path.GetDirectoryName(path)!)
                .GetAccessControl(AccessControlSections.Owner).GetOwner(typeof(SecurityIdentifier))?.Value;
            _canRead = TokenCacheFileSecurity.CanRead(stream.SafeFileHandle);
            if (_canRead)
            {
                using var reader = new StreamReader(stream);
                _contents = reader.ReadToEnd();
            }
        }
    }

    [Fact]
    void should_set_the_current_user_as_file_owner()
    {
        if (OperatingSystem.IsWindows())
        {
            _fileOwner.ShouldEqual(_currentUser);
        }
    }

    [Fact]
    void should_set_the_current_user_as_directory_owner()
    {
        if (OperatingSystem.IsWindows())
        {
            _directoryOwner.ShouldEqual(_currentUser);
        }
    }

    [Fact]
    void should_accept_the_written_cache()
    {
        if (OperatingSystem.IsWindows())
        {
            _canRead.ShouldBeTrue();
        }
    }

    [Fact]
    void should_read_back_the_written_contents()
    {
        if (OperatingSystem.IsWindows())
        {
            _contents.ShouldEqual(CacheContents);
        }
    }
}
