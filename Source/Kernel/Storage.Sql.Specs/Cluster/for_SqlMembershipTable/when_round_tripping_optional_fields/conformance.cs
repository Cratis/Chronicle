// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_round_tripping_optional_fields;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    MembershipTableData _after;
    async Task Because()
    {
        Entry.RoleName = null;
        Entry.SuspectTimes = null;
        Entry.SiloAddress = SiloAddress.New(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 11111), 1);
        _after = await Insert();
    }
    [Fact] public void should_preserve_a_null_role() => _after.Members.Single().Item1.RoleName.ShouldBeNull();
    [Fact] public void should_preserve_a_null_vote_list() => _after.Members.Single().Item1.SuspectTimes.ShouldBeNull();
    [Fact] public void should_round_trip_an_ipv4_address() => _after.Members.Single().Item1.SiloAddress.ShouldEqual(Entry.SiloAddress);
}
