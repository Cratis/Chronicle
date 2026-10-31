// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_RosterHistory;

public class when_a_grant_is_repeated_after_revocation : given.a_roster_history
{
    void Establish()
    {
        var original = Add(new UserGrantedAdministrator(_first, GrantReason.Bootstrap, "spec"));
        Add(new UserRevokedAdministrator(_first, "spec"));
        var repeated = Add(new UserGrantedAdministrator(_first, GrantReason.Bootstrap, "spec"));
        _contents[repeated] = _contents[original];
    }

    void Because() => Replay();

    [Fact] void should_suspend_privileges() => _history.Granted.ShouldBeEmpty();
    [Fact] void should_require_recovery() => _history.RecoveryRequired.ShouldBeTrue();
}
