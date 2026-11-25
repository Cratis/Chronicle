// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_TokenCacheFileSecurity;

public class when_windows_access_rules_do_not_grant_foreign_writes : given.windows_permissions
{
    void Because() => _canRead = WindowsTokenCachePermissions.IsTrusted(
        CurrentUser,
        CurrentUser,
        [
            (CurrentUser, FullControl, true),
            (ForeignUser, ReadAndExecute, true),
            (ForeignUser, FullControl, false)
        ]);

    [Fact] void should_trust_the_cache() => _canRead.ShouldBeTrue();
}
