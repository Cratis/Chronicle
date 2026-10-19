// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_cleaning_defunct_members;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    MembershipTableData _after;
    async Task Because()
    {
        for (var generation = 1; generation <= 4; generation++)
        {
            var entry = CreateEntry(generation);
            entry.Status = generation == 2 ? SiloStatus.Active : SiloStatus.Dead;
            if (generation >= 3) entry.IAmAliveTime = entry.IAmAliveTime.AddDays(2);
            (await Table.InsertRow(entry, (await Table.ReadAll()).Version.Next())).ShouldBeTrue();
        }
        await Table.CleanupDefunctSiloEntries(new DateTimeOffset(Entry.IAmAliveTime.AddDays(1)));
        _after = await Table.ReadAll();
    }
    [Fact] public void should_remove_only_old_non_active_members() => _after.Members.Select(member => member.Item1.SiloAddress.Generation).Order().ShouldEqual([2, 3, 4]);
    [Fact] public void should_preserve_the_version() => _after.Version.Version.ShouldEqual(4);
}
