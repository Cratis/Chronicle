// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Validates Windows token cache ownership and write access.
/// </summary>
internal static class WindowsTokenCachePermissions
{
    /// <summary>
    /// FILE_WRITE_DATA, FILE_APPEND_DATA, FILE_WRITE_EA, FILE_WRITE_ATTRIBUTES, FILE_DELETE_CHILD,
    /// DELETE, WRITE_DAC, WRITE_OWNER, GENERIC_WRITE and GENERIC_ALL from the Windows access mask contract:
    /// https://learn.microsoft.com/windows/win32/fileio/file-access-rights-constants.
    /// </summary>
    const int WriteAccess = 0x000D0156 | 0x50000000;

    /// <summary>
    /// Checks the permissions of the open cache file, failing closed if they cannot be read.
    /// </summary>
    /// <param name="handle">The open cache file.</param>
    /// <returns>Whether the cache file can be trusted.</returns>
    [SupportedOSPlatform("windows")]
    internal static bool CanRead(SafeFileHandle handle)
    {
        var referenceAdded = false;
        try
        {
            // Borrow the open handle without closing it or resolving the cache path again.
            handle.DangerousAddRef(ref referenceAdded);
            using var borrowedHandle = new SafeFileHandle(handle.DangerousGetHandle(), ownsHandle: false);
            using var stream = new FileStream(borrowedHandle, FileAccess.Read);
            using var identity = WindowsIdentity.GetCurrent();
            var security = stream.GetAccessControl();
            var descriptor = new RawSecurityDescriptor(security.GetSecurityDescriptorBinaryForm(), 0);
            IEnumerable<(string Sid, int Rights, bool Allow)>? rules = null;
            if (descriptor.DiscretionaryAcl is not null)
            {
                rules = security
                    .GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier))
                    .Cast<FileSystemAccessRule>()
                    .Select(rule => (rule.IdentityReference.Value, (int)rule.FileSystemRights, rule.AccessControlType == AccessControlType.Allow));
            }

            return IsTrusted(identity.User?.Value, security.GetOwner(typeof(SecurityIdentifier))?.Value, rules);
        }
        catch (Exception)
        {
            // Unverifiable ownership or permissions must never authorize reuse of a cache file.
            return false;
        }
        finally
        {
            if (referenceAdded)
            {
                handle.DangerousRelease();
            }
        }
    }

    /// <summary>
    /// Checks that only the current owner has any Allow ACE granting write access.
    /// </summary>
    /// <param name="currentUser">The current user's SID.</param>
    /// <param name="owner">The file owner's SID.</param>
    /// <param name="rules">The file's access rules, or null for an unrestricted DACL.</param>
    /// <returns>Whether the ownership and access rules are trusted.</returns>
    internal static bool IsTrusted(
        string? currentUser,
        string? owner,
        IEnumerable<(string Sid, int Rights, bool Allow)>? rules) =>
        currentUser is not null && owner == currentUser &&
        rules?.Any(rule => rule.Allow && rule.Sid != currentUser && (rule.Rights & WriteAccess) != 0) == false;
}
