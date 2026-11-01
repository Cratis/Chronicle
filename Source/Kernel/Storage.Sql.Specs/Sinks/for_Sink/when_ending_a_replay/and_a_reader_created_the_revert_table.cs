// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

public class and_a_reader_created_the_revert_table : for_Sink.given.an_accumulating_sql_read_model<SqlSinkHarness>
{
    Exception? _error;
    int? _primary;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await _sink.BeginReplay(ReplayContext());
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 42UL);
        await CreateLegacyTable(ReplayContext().RevertContainerName.Value);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _sink.EndReplay(ReplayContext()));
        _primary = await CurrentCountOrNull();
    }

    [Fact] void should_not_mistake_the_readers_table_for_a_completed_swap() => _error.ShouldBeOfExactType<ReadModelTableNameBindingFailure>();
    [Fact] void should_leave_the_original_primary_untouched() => _primary.ShouldEqual(1);
}
