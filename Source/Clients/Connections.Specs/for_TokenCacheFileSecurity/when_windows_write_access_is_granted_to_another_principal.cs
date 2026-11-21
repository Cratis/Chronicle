// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_TokenCacheFileSecurity;

public class when_windows_write_access_is_granted_to_another_principal : given.windows_permissions
{
    bool[] _results;

    void Because()
    {
        int[] rights =
        [
            0x00000002, // FILE_WRITE_DATA
            0x00000004, // FILE_APPEND_DATA
            0x00000010, // FILE_WRITE_EA
            0x00000100, // FILE_WRITE_ATTRIBUTES
            0x00010000, // DELETE
            0x00000040, // FILE_DELETE_CHILD
            0x00040000, // WRITE_DAC
            0x00080000, // WRITE_OWNER
            0x000301BF, // Modify
            FullControl,
            0x40000000, // GENERIC_WRITE
            0x10000000 // GENERIC_ALL
        ];
        string[] principals = [ForeignUser, "S-1-1-0", "S-1-5-18", "S-1-5-32-544"];
        _results = principals.SelectMany(principal => rights.Select(right => WindowsTokenCachePermissions.IsTrusted(
            CurrentUser, CurrentUser, [(CurrentUser, FullControl, true), (principal, right, true)]))).ToArray();
    }

    [Fact] void should_reject_every_foreign_write_grant() => _results.ShouldEachConformTo(result => !result);
}
