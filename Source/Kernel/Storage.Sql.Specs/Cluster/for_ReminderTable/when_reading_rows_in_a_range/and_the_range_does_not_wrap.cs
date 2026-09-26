// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.when_reading_rows_in_a_range;

public class and_the_range_does_not_wrap : given.three_reminders_on_the_ring
{
    async Task Because() => _result = await _table.ReadRows(LowestHash, HighestHash);

    [Fact] void should_return_two_reminders() => _result.Reminders.Count.ShouldEqual(2);
    [Fact] void should_exclude_the_reminder_on_the_lower_bound() => ReadGrainIds.ShouldNotContain(_lowest);
    [Fact] void should_include_the_reminder_inside_the_range() => ReadGrainIds.ShouldContain(_middle);
    [Fact] void should_include_the_reminder_on_the_upper_bound() => ReadGrainIds.ShouldContain(_highest);
}
