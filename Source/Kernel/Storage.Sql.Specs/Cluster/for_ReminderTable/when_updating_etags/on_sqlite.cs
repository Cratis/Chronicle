// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.when_updating_etags;

public class on_sqlite : given.a_reminder_table
{
    ReminderEntry _entry;
    string _oldETag;
    string _newETag;

    async Task Establish()
    {
        _entry = CreateEntry(GrainId.Create("observer", "etag-spec"));
        _oldETag = (await _table.UpsertRow(_entry))!;
        _entry.ETag = _oldETag;
    }

    async Task Because() => _newETag = (await _table.UpsertRow(_entry))!;

    [Fact] void should_mint_a_new_etag() => _newETag.ShouldNotEqual(_oldETag);
    [Fact] async Task should_reject_the_old_etag() => (await _table.RemoveRow(_entry.GrainId, _entry.ReminderName, _oldETag)).ShouldBeFalse();
    [Fact] async Task should_remove_with_the_new_etag() => (await _table.RemoveRow(_entry.GrainId, _entry.ReminderName, _newETag)).ShouldBeTrue();
}
