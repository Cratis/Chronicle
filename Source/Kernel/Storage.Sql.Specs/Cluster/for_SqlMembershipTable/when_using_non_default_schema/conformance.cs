// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_using_non_default_schema;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    protected override bool NonDefaultSchema => true;
    MembershipTableData _after;
    async Task Because()
    {
        await Table.InitializeMembershipTable(true);
        _after = await Insert();
    }
    [Fact] public void should_store_members_in_the_configured_schema() => _after.Members.Count.ShouldEqual(1);
    [Fact] public void should_update_the_configured_version() => _after.Version.Version.ShouldEqual(1);
}
