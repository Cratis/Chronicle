// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections.for_TokenCacheFileSecurity;

public class when_the_windows_owner_is_foreign : given.windows_permissions
{
    void Because() => _canRead = WindowsTokenCachePermissions.IsTrusted(
        CurrentUser, ForeignUser, [(CurrentUser, FullControl, true)]);

    [Fact] void should_treat_the_cache_as_a_miss() => _canRead.ShouldBeFalse();
}
