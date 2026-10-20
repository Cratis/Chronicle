// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.when_removing_a_legacy_row;

public class with_a_wrong_etag : given.a_reminder_table
{
    GrainId _grainId;
    bool _removed;

    async Task Establish()
    {
        _grainId = GrainId.Create("observer", new string('x', 250));
        var row = CreateEntry(_grainId).ToSql();
        row.Id = $"{_grainId}-retry";
        await using var context = CreateContext();
        context.Reminders.Add(row);
        await context.SaveChangesAsync();
    }

    async Task Because() => _removed = await _table.RemoveRow(_grainId, "retry", "wrong");

    [Fact] void should_reject_removal() => _removed.ShouldBeFalse();
    [Fact] async Task should_preserve_the_reminder() => (await _table.ReadRow(_grainId, "retry")).ShouldNotBeNull();
}
