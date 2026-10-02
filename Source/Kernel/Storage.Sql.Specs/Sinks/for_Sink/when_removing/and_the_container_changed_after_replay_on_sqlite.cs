// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_removing;

public class and_the_container_changed_after_replay_on_sqlite : given.a_replayed_read_model_with_a_changed_container<SqlSinkHarness>
{
    Task Because() => PruneOldReplay();

    [Fact] void should_drop_the_old_containers_backup() => _backupExists.ShouldBeFalse();
    [Fact] void should_remove_the_old_containers_marker() => _markerExists.ShouldBeFalse();
    [Fact] void should_preserve_the_current_container() => _currentCount.ShouldEqual(7);
}
