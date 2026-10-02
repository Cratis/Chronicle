// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.when_upserting;

public class and_an_ambiguous_legacy_key_belongs_to_another_reminder : given.a_reminder_table
{
    GrainId _legacyGrain;
    GrainId _newGrain;
    ReminderEntry _legacy;
    ReminderEntry _created;
    int _count;

    async Task Establish()
    {
        _legacyGrain = GrainId.Create("observer", new string('x', 250));
        _newGrain = GrainId.Create("observer", new string('x', 250) + "-part");
        var row = CreateEntry(_legacyGrain, "part-retry").ToSql();
        row.Id = $"{_legacyGrain}-part-retry";
        await using var context = CreateContext();
        context.Reminders.Add(row);
        await context.SaveChangesAsync();
    }

    async Task Because()
    {
        await _table.UpsertRow(CreateEntry(_newGrain));
        _legacy = (await _table.ReadRow(_legacyGrain, "part-retry"))!;
        _created = (await _table.ReadRow(_newGrain, "retry"))!;
        await using var context = CreateContext();
        _count = context.Reminders.Count();
    }

    [Fact] void should_preserve_the_legacy_reminder() => _legacy.GrainId.ShouldEqual(_legacyGrain);
    [Fact] void should_create_the_distinct_reminder() => _created.GrainId.ShouldEqual(_newGrain);
    [Fact] void should_keep_both_rows() => _count.ShouldEqual(2);
}
