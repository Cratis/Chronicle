// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.when_reading_rows_in_a_range;

public class and_the_range_wraps : given.three_reminders_on_the_ring
{
    async Task Because() => _result = await _table.ReadRows(MiddleHash, LowestHash);

    [Fact] void should_return_two_reminders() => _result.Reminders.Count.ShouldEqual(2);
    [Fact] void should_include_the_reminder_after_the_lower_bound() => ReadGrainIds.ShouldContain(_highest);
    [Fact] void should_include_the_reminder_on_the_upper_bound_past_the_wrap() => ReadGrainIds.ShouldContain(_lowest);
    [Fact] void should_exclude_the_reminder_on_the_lower_bound() => ReadGrainIds.ShouldNotContain(_middle);
}
