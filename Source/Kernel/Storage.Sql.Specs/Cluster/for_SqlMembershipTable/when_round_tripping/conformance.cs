// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_round_tripping;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    MembershipTableData _result;
    async Task Because() => _result = await Insert();
    [Fact] public void should_round_trip_all_fields()
    {
        var actual = _result.Members.Single().Item1;
        actual.SiloAddress.ShouldEqual(Entry.SiloAddress);
        actual.SiloName.ShouldEqual(Entry.SiloName);
        actual.HostName.ShouldEqual(Entry.HostName);
        actual.Status.ShouldEqual(Entry.Status);
        actual.ProxyPort.ShouldEqual(Entry.ProxyPort);
        actual.RoleName.ShouldEqual(Entry.RoleName);
        actual.UpdateZone.ShouldEqual(Entry.UpdateZone);
        actual.FaultZone.ShouldEqual(Entry.FaultZone);
        actual.StartTime.ShouldEqual(Entry.StartTime);
        actual.IAmAliveTime.ShouldEqual(Entry.IAmAliveTime);
        actual.SuspectTimes.Count.ShouldEqual(1);
        actual.SuspectTimes[0].Item1.ShouldEqual(Entry.SuspectTimes[0].Item1);
        actual.SuspectTimes[0].Item2.ShouldEqual(Entry.SuspectTimes[0].Item2);
        actual.StartTime.Kind.ShouldEqual(DateTimeKind.Utc);
        actual.IAmAliveTime.Kind.ShouldEqual(DateTimeKind.Utc);
    }
    [Fact] public void should_increment_the_version() => _result.Version.Version.ShouldEqual(1);
    [Fact] public async Task should_only_read_the_requested_row() => (await Table.ReadRow(CreateEntry(9).SiloAddress)).Members.Count.ShouldEqual(0);
}
