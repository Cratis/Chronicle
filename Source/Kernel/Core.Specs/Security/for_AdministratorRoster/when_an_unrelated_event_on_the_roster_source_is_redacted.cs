// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Security;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_an_unrelated_event_on_the_roster_source_is_redacted : given.an_administrator_roster
{
    EventSequenceNumber _tombstone;
    EventSequenceNumber _recoveryGrant;
    bool _recovered;
    bool _granted;

    async Task Establish()
    {
        await AddUser(_first, "operator");
        await AddUser(_second, "managed");
        _tombstone = SeedRedactedUnrelatedEvent(Roster.EventSourceId);
        ConfigureRecovery("operator", "fresh");
    }

    async Task Because()
    {
        await _roster.EnsureAdministratorPrivilege();
        _recoveryGrant = _appended.Last(@event => @event.Context.EventSourceId == Roster.EventSourceId).Context.SequenceNumber;
        await _roster.Grant(_second, GrantReason.AdministratorGrant, "spec");
        _recovered = await CreateRoster().IsActiveAdministrator(_first);
        _granted = await CreateRoster().IsActiveAdministrator(_second);
    }

    [Fact] void should_complete_recovery() => _recovered.ShouldBeTrue();
    [Fact] void should_append_the_grant() => _granted.ShouldBeTrue();
    [Fact] void should_not_hit_concurrency_violations() => _conflicts.ShouldEqual(0);
    [Fact] void should_expect_the_tombstone_as_the_roster_tail_for_recovery() => _scopes[1].Scopes[Roster.EventSourceId].SequenceNumber.ShouldEqual(_tombstone);
    [Fact] void should_expect_the_recovery_grant_as_the_roster_tail_for_the_grant() => _scopes[^1].Scopes[Roster.EventSourceId].SequenceNumber.ShouldEqual(_recoveryGrant);
}
