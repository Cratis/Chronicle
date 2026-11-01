// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_an_unsigned_marker_precedes_migration : given.an_administrator_roster
{
    int _automaticAppends;
    bool _recovered;

    async Task Establish()
    {
        await AddUser(_first, "root");
        Seed(_first, new InitialAdminUserAdded("root", "root@example.com"));
        SeedForged(Roster.MigrationEventSourceId, new AdministratorPrivilegeMigrated("Proven", _first));
        await _roster.EnsureAdministratorPrivilege();
        _automaticAppends = _appendCalls;
        ConfigureRecovery("root", "fresh");
    }

    async Task Because()
    {
        await _roster.EnsureAdministratorPrivilege();
        _recovered = await _roster.IsActiveAdministrator(_first);
    }

    [Fact] void should_not_attempt_automatic_migration() => _automaticAppends.ShouldEqual(0);
    [Fact] void should_allow_operator_recovery() => _recovered.ShouldBeTrue();
}
