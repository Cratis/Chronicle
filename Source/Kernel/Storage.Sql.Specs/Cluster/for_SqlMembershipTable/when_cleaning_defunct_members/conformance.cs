// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_cleaning_defunct_members;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    MembershipTableData _after;
    async Task Because()
    {
        var cutoff = Entry.IAmAliveTime.AddDays(1);
        for (var generation = 1; generation <= 9; generation++)
        {
            var entry = CreateEntry(generation);
            entry.Status = generation switch
            {
                2 => SiloStatus.Active,
                3 => SiloStatus.Joining,
                4 => SiloStatus.ShuttingDown,
                _ => SiloStatus.Dead
            };
            if (generation == 5) entry.IAmAliveTime = cutoff.AddDays(1);
            if (generation == 6) entry.StartTime = cutoff.AddDays(1);
            if (generation == 7) entry.SuspectTimes.Add(Tuple.Create(Entry.SiloAddress, cutoff.AddDays(1)));
            if (generation == 8) entry.SuspectTimes.Add(Tuple.Create(Entry.SiloAddress, cutoff));
            if (generation == 9) entry.SuspectTimes = null;
            (await Table.InsertRow(entry, (await Table.ReadAll()).Version.Next())).ShouldBeTrue();
        }
        await Table.CleanupDefunctSiloEntries(new DateTimeOffset(cutoff));
        _after = await Table.ReadAll();
    }
    [Fact] public void should_remove_only_dead_members_with_all_times_before_the_cutoff() => _after.Members.Select(member => member.Item1.SiloAddress.Generation).Order().ShouldEqual([2, 3, 4, 5, 6, 7, 8]);
    [Fact] public void should_preserve_the_version() => _after.Version.Version.ShouldEqual(9);
}
