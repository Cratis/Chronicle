// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_updating_i_am_alive;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    MembershipTableData _before;
    MembershipTableData _after;
    async Task Because()
    {
        _before = await Insert();
        Entry.IAmAliveTime = Entry.IAmAliveTime.AddMinutes(1);
        Entry.Status = SiloStatus.Active;
        await Table.UpdateIAmAlive(Entry);
        _after = await Table.ReadAll();
    }
    [Fact] public void should_preserve_the_version() => _after.Version.Version.ShouldEqual(_before.Version.Version);
    [Fact] public void should_preserve_the_etag() => _after.Members.Single().Item2.ShouldEqual(_before.Members.Single().Item2);
    [Fact] public void should_change_the_heartbeat() => _after.Members.Single().Item1.IAmAliveTime.ShouldEqual(Entry.IAmAliveTime);
    [Fact] public void should_not_change_other_fields() => _after.Members.Single().Item1.Status.ShouldEqual(SiloStatus.Joining);
}
