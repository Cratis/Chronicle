// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.when_cleaning_while_members_change;

public abstract class conformance(MembershipHarness harness) : a_membership_table(harness)
{
    MembershipTableData _after;
    bool _updated;

    async Task Because()
    {
        var cutoff = Entry.IAmAliveTime.AddDays(1);
        Entry.Status = SiloStatus.Dead;
        await Insert();
        var heartbeat = CreateEntry(2);
        heartbeat.Status = SiloStatus.Dead;
        (await Table.InsertRow(heartbeat, (await Table.ReadAll()).Version.Next())).ShouldBeTrue();
        var cleaner = WithInterceptors(new WriteBeforeCleanup(async () =>
        {
            var current = await Table.ReadRow(Entry.SiloAddress);
            Entry.SuspectTimes.Add(Tuple.Create(heartbeat.SiloAddress, cutoff.AddDays(1)));
            _updated = await Table.UpdateRow(Entry, current.Members.Single().Item2, current.Version.Next());
            heartbeat.IAmAliveTime = cutoff.AddDays(1);
            await Table.UpdateIAmAlive(heartbeat);
        }));
        await cleaner.CleanupDefunctSiloEntries(new DateTimeOffset(cutoff));
        _after = await Table.ReadAll();
    }

    [Fact] public void should_have_committed_the_concurrent_update() => _updated.ShouldBeTrue();
    [Fact] public void should_preserve_the_member_with_a_new_suspect_vote() => _after.Members.Any(member => member.Item1.SiloAddress.Generation == 1).ShouldBeTrue();
    [Fact] public void should_preserve_the_member_with_a_new_heartbeat() => _after.Members.Any(member => member.Item1.SiloAddress.Generation == 2).ShouldBeTrue();
}
