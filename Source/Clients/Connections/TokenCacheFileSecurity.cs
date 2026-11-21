// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Creates private token cache files and validates cache permissions and ownership.
/// </summary>
internal static class TokenCacheFileSecurity
{
    const UnixFileMode FileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
    const UnixFileMode DirectoryMode = FileMode | UnixFileMode.UserExecute;

    /// <summary>
    /// Creates a private cache directory, restricting existing Unix directories owned by this user.
    /// </summary>
    /// <param name="path">The cache directory.</param>
    internal static void CreateDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            var security = new DirectorySecurity();
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            security.AddAccessRule(new FileSystemAccessRule(
                WindowsIdentity.GetCurrent().User!,
                FileSystemRights.FullControl,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow));
            new DirectoryInfo(path).Create(security);
            return;
        }

        Directory.CreateDirectory(path, DirectoryMode);
        using var handle = UnixTokenCacheOwnership.OpenDirectory(path);
        if (UnixTokenCacheOwnership.IsCurrentUser(handle))
        {
            File.SetUnixFileMode(handle, DirectoryMode);
        }
    }

    /// <summary>
    /// Exclusively creates a file with owner-only permissions before any bytes are written.
    /// </summary>
    /// <param name="path">The temporary file path.</param>
    /// <returns>The writable private file.</returns>
    internal static FileStream CreateFile(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return CreateWindowsFile(path);
        }

        return new FileStream(path, new FileStreamOptions
        {
            Mode = System.IO.FileMode.CreateNew,
            Access = FileAccess.Write,
            UnixCreateMode = FileMode
        });
    }

    /// <summary>
    /// Checks whether an open cache file has trusted permissions and ownership.
    /// </summary>
    /// <param name="handle">The open cache file.</param>
    /// <returns>Whether the file can be trusted.</returns>
    internal static bool CanRead(SafeFileHandle handle)
    {
        if (OperatingSystem.IsWindows())
        {
            return WindowsTokenCachePermissions.CanRead(handle);
        }

        const UnixFileMode shared = UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                                    UnixFileMode.OtherRead | UnixFileMode.OtherWrite | UnixFileMode.OtherExecute;
        return (File.GetUnixFileMode(handle) & shared) == UnixFileMode.None && UnixTokenCacheOwnership.IsCurrentUser(handle);
    }

    [SupportedOSPlatform("windows")]
    static FileStream CreateWindowsFile(string path)
    {
        var security = new FileSecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new FileSystemAccessRule(
            WindowsIdentity.GetCurrent().User!, FileSystemRights.FullControl, AccessControlType.Allow));
        return new FileInfo(path).Create(
            System.IO.FileMode.CreateNew,
            FileSystemRights.FullControl,
            FileShare.None,
            4096,
            FileOptions.None,
            security);
    }
}
