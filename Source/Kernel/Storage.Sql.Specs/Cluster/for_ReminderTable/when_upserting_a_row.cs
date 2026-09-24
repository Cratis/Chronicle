// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable;

public class when_upserting_a_row : given.a_reminder_table
{
    GrainId _grainId;
    string? _returnedETag;
    Reminder _storedReminder;

    void Establish() => _grainId = GrainId.Create("observer", "some-observer");

    async Task Because()
    {
        _returnedETag = await _table.UpsertRow(CreateEntry(_grainId));
        await using var context = CreateContext();
        _storedReminder = context.Reminders.Single();
    }

    [Fact] void should_return_the_etag_that_was_stored() => _returnedETag.ShouldEqual(_storedReminder.ETag);
    [Fact] void should_store_the_reminder_under_the_uniform_hash_of_the_grain() => _storedReminder.GrainHash.ShouldEqual(_grainId.GetUniformHashCode());
}
