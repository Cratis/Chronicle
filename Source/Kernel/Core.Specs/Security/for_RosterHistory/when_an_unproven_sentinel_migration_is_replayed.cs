// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_RosterHistory;

public class when_an_unproven_sentinel_migration_is_replayed : given.a_roster_history
{
    bool _completedBeforeRestart;
    bool _grantedBeforeRestart;

    void Establish() => Add(new AdministratorPrivilegeMigrated("Unproven", UserId.NotSet));

    void Because()
    {
        Replay();
        _completedBeforeRestart = _history.MigrationCompleted;
        _grantedBeforeRestart = _history.Granted.Count != 0;
        _history = new RosterHistory(_proof);
        Replay();
    }

    [Fact] void should_accept_the_signed_marker() => _completedBeforeRestart.ShouldBeTrue();
    [Fact] void should_not_grant_privileges() => _grantedBeforeRestart.ShouldBeFalse();
    [Fact] void should_complete_migration_after_restart() => _history.MigrationCompleted.ShouldBeTrue();
    [Fact] void should_not_grant_privileges_after_restart() => _history.Granted.ShouldBeEmpty();
    [Fact] void should_not_establish_any_grant_provenance() => _history.EverGranted.ShouldBeEmpty();
    [Fact] void should_verify_the_unchanged_proof_body() => _history.Compromised.ShouldBeFalse();
    [Fact] void should_advance_the_migration_tail() => _history.RawMigrationTail.ShouldEqual(_events.Single().Context.SequenceNumber);
}
