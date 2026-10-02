// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

public class and_an_empty_replay_has_a_markerless_backup : for_Sink.given.an_accumulating_sql_read_model<SqlSinkHarness>
{
    Exception? _error;
    int? _primary;
    bool _backupExists;
    bool _replayExists;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await _sink.BeginReplay(ReplayContext());
        await _sink.GetInstances(ReplayContext().RevertContainerName);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _sink.EndReplay(ReplayContext()));
        _primary = await CurrentCountOrNull();
        _backupExists = await TableExists(ReplayContext().RevertContainerName.Value);
        _replayExists = await TableExists($"replay-{ContainerName}");
    }

    [Fact] void should_complete_without_publishing_a_replay() => _error.ShouldBeNull();
    [Fact] void should_preserve_the_primary() => _primary.ShouldEqual(1);
    [Fact] void should_leave_the_markerless_backup_untouched() => _backupExists.ShouldBeTrue();
    [Fact] void should_drop_only_the_empty_replay_table() => _replayExists.ShouldBeFalse();
}
