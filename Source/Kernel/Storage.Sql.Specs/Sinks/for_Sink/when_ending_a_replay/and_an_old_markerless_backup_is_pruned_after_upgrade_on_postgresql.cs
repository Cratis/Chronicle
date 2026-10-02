// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

[Collection(PostgreSqlCollection.Name)]
public class and_an_old_markerless_backup_is_pruned_after_upgrade_on_postgresql(PostgreSqlFixture fixture) : given.a_read_model_with_a_markerless_backup<PostgreSqlSinkHarness>
{
    protected override PostgreSqlSinkHarness CreateSqlHarness() => new() { Fixture = fixture };

    Task Because() => PromoteAndPrune();

    [Fact] void should_have_reproduced_a_markerless_old_backup() => _removedMarkers.ShouldEqual(1);
    [Fact] void should_complete_the_new_replay() => _error.ShouldBeNull();
    [Fact] void should_publish_the_new_replay() => _primary.ShouldEqual(3);
    [Fact] void should_drop_the_old_markerless_backup() => _oldBackupExists.ShouldBeFalse();
    [Fact] void should_keep_only_the_new_replays_marker() => _remainingMarkers.ShouldEqual(1);
}
