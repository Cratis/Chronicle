// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_inserting_a_duplicate;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    bool _result;
    MembershipTableData _after;
    async Task Because()
    {
        var current = await Insert();
        _result = await Table.InsertRow(Entry, current.Version.Next());
        _after = await Table.ReadAll();
    }
    [Fact] public void should_reject_the_duplicate() => _result.ShouldBeFalse();
    [Fact] public void should_roll_back_the_version() => _after.Version.Version.ShouldEqual(1);
    [Fact] public void should_keep_one_member() => _after.Members.Count.ShouldEqual(1);
}
