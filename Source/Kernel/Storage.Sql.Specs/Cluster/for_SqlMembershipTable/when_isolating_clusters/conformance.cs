// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_isolating_clusters;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    MembershipTableData _original;
    MembershipTableData _other;
    async Task Because()
    {
        await Insert();
        var other = new SqlMembershipTable(Database, Options.Create(new ClusterOptions { ClusterId = "Membership-spec" }));
        await other.InitializeMembershipTable(true);
        (await other.InsertRow(Entry, (await other.ReadAll()).Version.Next())).ShouldBeTrue();
        await Table.DeleteMembershipTableEntries(ClusterId);
        _original = await Table.ReadAll();
        _other = await other.ReadAll();
    }
    [Fact] public void should_delete_only_the_requested_cluster() => _original.Members.Count.ShouldEqual(0);
    [Fact] public void should_keep_case_distinct_cluster_members() => _other.Members.Count.ShouldEqual(1);
    [Fact] public void should_keep_the_other_clusters_version() => _other.Version.Version.ShouldEqual(1);
}
