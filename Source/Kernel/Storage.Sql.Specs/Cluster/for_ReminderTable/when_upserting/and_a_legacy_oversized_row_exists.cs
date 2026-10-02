// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.when_upserting;

public class and_a_legacy_oversized_row_exists : given.a_reminder_table
{
    GrainId _grainId;
    ReminderEntry _legacy;
    ReminderEntry _updated;
    int _rowCount;
    bool _removed;

    async Task Establish()
    {
        _grainId = GrainId.Create("observer", new string('x', 250));
        var entry = CreateEntry(_grainId);
        var row = entry.ToSql();
        row.Id = $"{_grainId}-retry";
        await using var context = CreateContext();
        context.Reminders.Add(row);
        await context.SaveChangesAsync();
        _legacy = (await _table.ReadRow(_grainId, "retry"))!;
    }

    async Task Because()
    {
        var entry = CreateEntry(_grainId);
        entry.Period = TimeSpan.FromMinutes(5);
        var tag = await _table.UpsertRow(entry);
        _updated = (await _table.ReadRow(_grainId, "retry"))!;
        await using var context = CreateContext();
        _rowCount = context.Reminders.Count();
        _removed = await _table.RemoveRow(_grainId, "retry", tag!);
    }

    [Fact] void should_read_the_legacy_row() => _legacy.GrainId.ShouldEqual(_grainId);
    [Fact] void should_not_create_a_second_row() => _rowCount.ShouldEqual(1);
    [Fact] void should_update_the_existing_reminder() => _updated.Period.ShouldEqual(TimeSpan.FromMinutes(5));
    [Fact] void should_remove_the_legacy_row_by_etag() => _removed.ShouldBeTrue();
}
