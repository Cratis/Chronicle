// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_not_initializing_the_version;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    MembershipTableData _after;
    bool _inserted;
    async Task Because()
    {
        await Table.DeleteMembershipTableEntries(ClusterId);
        await Table.InitializeMembershipTable(false);
        _after = await Table.ReadAll();
        _inserted = await Table.InsertRow(Entry, _after.Version.Next());
    }
    [Fact] public void should_read_an_empty_uninitialized_table() => _after.Members.Count.ShouldEqual(0);
    [Fact] public void should_not_allow_a_cas_without_a_version_row() => _inserted.ShouldBeFalse();
}
