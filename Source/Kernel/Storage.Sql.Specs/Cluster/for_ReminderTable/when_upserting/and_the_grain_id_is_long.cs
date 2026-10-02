// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.when_upserting;

public class and_the_grain_id_is_long : given.a_reminder_table
{
    GrainId _grainId;
    string _key;
    ReminderEntry _read;
    bool _staleRemoval;
    bool _removed;

    void Establish() => _grainId = GrainId.Create("observer", new string('界', 300));

    async Task Because()
    {
        var tag = await _table.UpsertRow(CreateEntry(_grainId, "chronicle-observer:alerts"));
        await using (var context = CreateContext())
        {
            _key = context.Reminders.Single().Id;
        }
        _read = (await _table.ReadRow(_grainId, "chronicle-observer:alerts"))!;
        _staleRemoval = await _table.RemoveRow(_grainId, "chronicle-observer:alerts", "stale");
        _removed = await _table.RemoveRow(_grainId, "chronicle-observer:alerts", tag!);
    }

    [Fact] void should_fit_the_key_limit_enforced_by_every_provider() => _key.Length.ShouldBeLessThan(201);
    [Fact] void should_read_the_original_grain_identity() => _read.GrainId.ShouldEqual(_grainId);
    [Fact] void should_reject_a_stale_etag() => _staleRemoval.ShouldBeFalse();
    [Fact] void should_remove_with_the_stored_etag() => _removed.ShouldBeTrue();
}
