// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Security;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security.for_RosterHistory;

public class when_the_predecessor_does_not_match : given.a_roster_history
{
    void Establish()
    {
        Add(new UserGrantedAdministrator(_first, GrantReason.Bootstrap, "spec"));
        var slot = Add(new UserGrantedAdministrator(_second, GrantReason.AdministratorGrant, "spec"));
        _contents[slot] = Signed(new UserGrantedAdministrator(_second, GrantReason.AdministratorGrant, "spec"), Roster.EventSourceId, EventSequenceNumber.BeforeFirst, _proof);
    }

    void Because() => Replay();

    [Fact] void should_suspend_all_privileges() => _history.Granted.ShouldBeEmpty();
    [Fact] void should_require_recovery() => _history.RecoveryRequired.ShouldBeTrue();
}
