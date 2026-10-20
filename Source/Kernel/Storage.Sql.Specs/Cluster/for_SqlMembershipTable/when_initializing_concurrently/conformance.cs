// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_initializing_concurrently;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    MembershipTableData _after;
    async Task Because()
    {
        await Table.DeleteMembershipTableEntries(ClusterId);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Table.InitializeMembershipTable(true)));
        (await Table.InsertRow(Entry, (await Table.ReadAll()).Version.Next())).ShouldBeTrue();
        await Table.InitializeMembershipTable(true);
        _after = await Table.ReadAll();
    }
    [Fact] public void should_not_reset_an_existing_version() => _after.Version.Version.ShouldEqual(1);
    [Fact] public void should_preserve_the_inserted_member() => _after.Members.Count.ShouldEqual(1);
}
