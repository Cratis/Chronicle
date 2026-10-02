// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.when_upserting;

public class and_the_cluster_schema_was_wiped : given.a_wiped_reminder_table
{
    string? _etag;

    async Task Because() => _etag = await _table.UpsertRow(CreateEntry(_grainId, ReminderName));

    [Fact] async Task should_persist_the_new_reminder() => (await _table.ReadRow(_grainId, ReminderName))!.ETag.ShouldEqual(_etag);
}
