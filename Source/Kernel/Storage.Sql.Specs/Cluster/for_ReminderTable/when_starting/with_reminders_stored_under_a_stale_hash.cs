// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.when_starting;

public class with_reminders_stored_under_a_stale_hash : given.a_reminder_table
{
    GrainId _grainId;
    uint _grainHash;
    Reminder _storedReminder;
    ReminderTableData _rangeHoldingTheGrain;

    async Task Establish()
    {
        _grainId = GrainId.Create("observer", "some-observer");
        _grainHash = _grainId.GetUniformHashCode();
        await _table.UpsertRow(CreateEntry(_grainId));

        // Earlier versions stored a per-process hash that does not match the grain's position on the ring.
        await using var context = CreateContext();
        context.Reminders.Single().GrainHash = _grainHash + 1;
        await context.SaveChangesAsync();
    }

    async Task Because()
    {
        await _table.StartAsync(CancellationToken.None);
        await using var context = CreateContext();
        _storedReminder = context.Reminders.Single();
        _rangeHoldingTheGrain = await _table.ReadRows(_grainHash - 1, _grainHash);
    }

    [Fact] void should_store_the_reminder_under_the_uniform_hash_of_the_grain() => _storedReminder.GrainHash.ShouldEqual(_grainHash);
    [Fact] void should_read_the_reminder_in_a_range_holding_the_grain() => _rangeHoldingTheGrain.Reminders.Single().GrainId.ShouldEqual(_grainId);
}
