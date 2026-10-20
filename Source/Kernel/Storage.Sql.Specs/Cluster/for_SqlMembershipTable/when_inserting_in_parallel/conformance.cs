// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_inserting_in_parallel;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    bool[] _results;
    MembershipTableData _after;
    async Task Because()
    {
        var current = await Table.ReadAll();
        _results = await Task.WhenAll(Enumerable.Range(1, 8).Select(generation => Table.InsertRow(CreateEntry(generation), current.Version.Next())));
        _after = await Table.ReadAll();
    }
    [Fact] public void should_have_one_cluster_version_winner() => _results.Count(result => result).ShouldEqual(1);
    [Fact] public void should_insert_one_member() => _after.Members.Count.ShouldEqual(1);
    [Fact] public void should_increment_once() => _after.Version.Version.ShouldEqual(1);
}
