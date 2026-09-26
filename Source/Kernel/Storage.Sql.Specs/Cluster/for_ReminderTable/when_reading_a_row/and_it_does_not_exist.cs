// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_ReminderTable.when_reading_a_row;

public class and_it_does_not_exist : given.a_reminder_table
{
    ReminderEntry? _result;
    Exception? _error;

    async Task Because() => _error = await Catch.Exception(async () => _result = await _table.ReadRow(GrainId.Create("observer", "some-observer"), "retry"));

    [Fact] void should_not_fail() => _error.ShouldBeNull();
    [Fact] void should_return_nothing() => _result.ShouldBeNull();
}
