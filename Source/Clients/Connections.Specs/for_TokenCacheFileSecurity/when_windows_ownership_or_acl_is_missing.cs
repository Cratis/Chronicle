// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_TokenCacheFileSecurity;

public class when_windows_ownership_or_acl_is_missing : given.windows_permissions
{
    bool _withoutOwner;
    bool _withoutCurrentUser;
    bool _withoutAcl;

    void Because()
    {
        (string, int, bool)[] rules = [(CurrentUser, FullControl, true)];
        _withoutOwner = WindowsTokenCachePermissions.IsTrusted(CurrentUser, null, rules);
        _withoutCurrentUser = WindowsTokenCachePermissions.IsTrusted(null, null, rules);
        _withoutAcl = WindowsTokenCachePermissions.IsTrusted(CurrentUser, CurrentUser, null);
    }

    [Fact] void should_reject_missing_ownership() => _withoutOwner.ShouldBeFalse();
    [Fact] void should_reject_a_missing_current_user() => _withoutCurrentUser.ShouldBeFalse();
    [Fact] void should_reject_an_unrestricted_acl() => _withoutAcl.ShouldBeFalse();
}
