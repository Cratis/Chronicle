// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_updating_with_a_stale_version;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    bool _result;
    MembershipTableData _after;
    async Task Because()
    {
        var current = await Insert();
        Entry.Status = SiloStatus.Active;
        _result = await Table.UpdateRow(Entry, current.Members.Single().Item2, new TableVersion(2, "0"));
        _after = await Table.ReadAll();
    }
    [Fact] public void should_reject_the_stale_version() => _result.ShouldBeFalse();
    [Fact] public void should_preserve_the_member() => _after.Members.Single().Item1.Status.ShouldEqual(SiloStatus.Joining);
    [Fact] public void should_preserve_the_version() => _after.Version.Version.ShouldEqual(1);
}
