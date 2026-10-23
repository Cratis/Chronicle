// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_using_resolved_identity;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    MembershipTableData _after;
    async Task Because()
    {
        Harness.UseResolvedIdentity();
        await Table.InitializeMembershipTable(true);
        _after = await Insert();
    }
    [Fact] public void should_store_members_with_the_resolved_identity() => _after.Members.Count.ShouldEqual(1);
    [Fact] public void should_update_the_resolved_database_version() => _after.Version.Version.ShouldEqual(1);
    [Fact] public void should_resolve_identity_for_every_new_connection() => (Harness.IdentityResolutions >= 4).ShouldBeTrue();
}
