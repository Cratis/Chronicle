// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.when_reading_rows_in_a_range;

/// <summary>
/// A single silo owns the whole ring and reads it as a range whose bounds are equal - for instance (0, 0].
/// </summary>
public class and_the_range_covers_the_whole_ring : given.three_reminders_on_the_ring
{
    async Task Because() => _result = await _table.ReadRows(0, 0);

    [Fact] void should_return_every_reminder() => ReadGrainIds.ShouldContainOnly(_lowest, _middle, _highest);
}
