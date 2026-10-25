// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Security;

namespace Cratis.Chronicle.Security.for_AdministratorRoster;

public class when_migration_precedes_the_successful_bootstrap_grant : given.an_administrator_roster
{
    async Task Establish()
    {
        await AddUser(_first, "root");
        Seed(_first, new InitialAdminUserAdded("root", "root@example.com"));
    }

    async Task Because()
    {
        await _roster.EnsureAdministratorPrivilege();
        await _roster.Grant(_first, GrantReason.Bootstrap, "successful initial creation");
        await CreateRoster().Grant(_first, GrantReason.Bootstrap, "retried initial creation");
    }

    [Fact] void should_record_the_successful_bootstrap_authority_exactly_once() => Recorded<UserGrantedAdministrator>().Count(grant => grant.Reason == GrantReason.Bootstrap).ShouldEqual(1);
    [Fact] void should_retain_the_upgrade_provenance_audit() => Recorded<UserGrantedAdministrator>().Count(grant => grant.Reason == GrantReason.UpgradeProvenance).ShouldEqual(1);
}
