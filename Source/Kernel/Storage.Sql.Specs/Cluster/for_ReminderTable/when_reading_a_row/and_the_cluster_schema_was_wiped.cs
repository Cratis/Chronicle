// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.when_reading_a_row;

public class and_the_cluster_schema_was_wiped : given.a_wiped_reminder_table
{
    ReminderEntry? _result;

    async Task Because() => _result = await _table.ReadRow(_grainId, ReminderName);

    [Fact] void should_have_no_reminder_from_before_the_reset() => _result.ShouldBeNull();
}
