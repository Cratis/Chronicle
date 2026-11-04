// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_RosterHistory;

public class when_a_migration_marker_has_no_kernel_proof : given.a_roster_history
{
    void Establish()
    {
        Add(new UserGrantedAdministrator(_first, GrantReason.Bootstrap, "spec"));
        Add(new AdministratorPrivilegeMigrated("completed", _first), omitProof: true);
    }

    void Because() => Replay();

    [Fact] void should_suspend_all_privileges() => _history.Granted.ShouldBeEmpty();
    [Fact] void should_not_trust_the_marker() => _history.MigrationCompleted.ShouldBeFalse();
    [Fact] void should_advance_the_raw_marker_tail() => _history.RawMigrationTail.ShouldEqual(_events[^1].Context.SequenceNumber);
}
