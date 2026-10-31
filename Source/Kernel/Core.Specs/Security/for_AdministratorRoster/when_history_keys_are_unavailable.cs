// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;
using Microsoft.AspNetCore.DataProtection;
using Roster = Cratis.Chronicle.Concepts.Security.AdministratorRoster;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_history_keys_are_unavailable : given.an_administrator_roster
{
    int _automaticAppends;
    int _afterRecoveryAppends;
    bool _before;
    bool _recovered;

    async Task Establish()
    {
        await AddUser(_first, "root");
        await AddUser(_second, "operator");
        Seed(_first, new InitialAdminUserAdded("root", "root@example.com"));
        SeedGrant(_first);
        Seed(Roster.EventSourceId, new UserRevokedAdministrator(_first, "spec"));
        Seed(Roster.MigrationEventSourceId, new AdministratorPrivilegeMigrated("Proven", _first));
        _proof = new KernelEventProof(new EphemeralDataProtectionProvider());
        _roster = CreateRoster();
        _before = await _roster.IsActiveAdministrator(_first);
        await _roster.EnsureAdministratorPrivilege();
        await _roster.Grant(_first, GrantReason.Bootstrap, "spec");
        _automaticAppends = _appendCalls;
        ConfigureRecovery("operator", "fresh");
    }

    async Task Because()
    {
        await _roster.EnsureAdministratorPrivilege();
        _recovered = await _roster.IsActiveAdministrator(_second);
        _afterRecoveryAppends = _appendCalls;
        await _roster.Grant(_first, GrantReason.Bootstrap, "spec");
    }

    [Fact] void should_suspend_privileges() => _before.ShouldBeFalse();
    [Fact] void should_not_append_automatic_grants() => _automaticAppends.ShouldEqual(0);
    [Fact] void should_allow_operator_recovery() => _recovered.ShouldBeTrue();
    [Fact] void should_keep_automatic_grants_disabled() => _appendCalls.ShouldEqual(_afterRecoveryAppends);
}
