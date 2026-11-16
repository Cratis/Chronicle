// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Inspects the owner of open Unix cache files without a second path lookup.
/// </summary>
internal static partial class UnixTokenCacheOwnership
{
    /// <summary>
    /// Opens a Unix directory for ownership checks and permission changes.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <returns>The directory descriptor.</returns>
    internal static SafeFileHandle OpenDirectory(string path) => new(Open(path, 0), ownsHandle: true);

    /// <summary>
    /// Checks that a descriptor belongs to the effective user of this process.
    /// </summary>
    /// <param name="handle">The open descriptor.</param>
    /// <returns>Whether its owner is the current effective user.</returns>
    internal static bool IsCurrentUser(SafeFileHandle handle)
    {
        if (handle.IsInvalid)
        {
            return false;
        }

        if (OperatingSystem.IsLinux())
        {
            // AT_EMPTY_PATH inspects the open descriptor; STATX_UID requests its owner.
            return Statx(handle, string.Empty, 0x1000, 0x8, out var status) == 0 &&
                   (status.Mask & 0x8) != 0 && status.UserId == GetEffectiveUserId();
        }

        return OperatingSystem.IsMacOS() && IsCurrentMacUser(handle);
    }

    static bool IsCurrentMacUser(SafeFileHandle handle)
    {
        var result = RuntimeInformation.ProcessArchitecture == Architecture.X64
            ? FStatMacIntel(handle, out var status)
            : FStatMac(handle, out status);
        return result == 0 && status.UserId == GetEffectiveUserId();
    }

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "open", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int Open(string path, int flags);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "geteuid")]
    private static partial uint GetEffectiveUserId();

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "statx", StringMarshalling = StringMarshalling.Utf8)]
    private static partial int Statx(SafeFileHandle handle, string path, int flags, uint mask, out LinuxFileStatus status);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "fstat")]
    private static partial int FStatMac(SafeFileHandle handle, out MacFileStatus status);

    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [LibraryImport("libc", EntryPoint = "fstat$INODE64")]
    private static partial int FStatMacIntel(SafeFileHandle handle, out MacFileStatus status);

    /// <summary>
    /// Native statx has a fixed 256-byte ABI on every Linux architecture.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 256)]
    struct LinuxFileStatus
    {
        [FieldOffset(0)]
        public uint Mask;

        [FieldOffset(20)]
        public uint UserId;
    }

    /// <summary>
    /// Darwin's 64-bit inode stat ABI is shared by Intel and Apple Silicon.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 144)]
    struct MacFileStatus
    {
        [FieldOffset(16)]
        public uint UserId;
    }
}
