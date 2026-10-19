// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_deleting_and_initializing;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    MembershipTableData _after;
    async Task Because()
    {
        await Insert();
        await Table.DeleteMembershipTableEntries(ClusterId);
        await Table.InitializeMembershipTable(true);
        _after = await Table.ReadAll();
    }
    [Fact] public void should_recreate_version_zero() => _after.Version.Version.ShouldEqual(0);
    [Fact] public void should_delete_all_members() => _after.Members.Count.ShouldEqual(0);
    [Fact] public async Task should_allow_inserting_again() => (await Table.InsertRow(Entry, _after.Version.Next())).ShouldBeTrue();
}
