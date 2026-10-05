// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

[Collection(PostgreSqlCollection.Name)]
public class and_a_markerless_long_name_backup_collides_on_postgresql(PostgreSqlFixture fixture) : given.a_read_model_with_a_markerless_backup<PostgreSqlSinkHarness>
{
    static readonly string _longName = new('x', 54);
    int? _preservedCount;
    int? _currentBackupCount;
    string[] _preservedNames;

    protected override PostgreSqlSinkHarness CreateSqlHarness() => new() { Fixture = fixture };

    protected override ReadModelDefinition CreateReadModelDefinition() => base.CreateReadModelDefinition() with { ContainerName = _longName };

    async Task Because()
    {
        await PromoteNewReplay();
        if (_error is null)
        {
            await _sink.Remove(_oldReplay.RevertContainerName);
        }
        await using var context = OpenInspectionContext();
        _preservedNames = await context.Database.SqlQuery<string>($"SELECT tablename AS \"Value\" FROM pg_tables WHERE schemaname = current_schema()")
            .Where(name => name.StartsWith("chronicle_replay_backup_")).ToArrayAsync();
        if (_preservedNames.Length == 1)
        {
            _preservedCount = await StoredCount(_preservedNames[0]);
        }
        _currentBackupCount = await StoredCount(_newReplay.RevertContainerName.Value);
    }

    [Fact] void should_have_removed_the_old_versions_marker() => _removedMarkers.ShouldEqual(1);
    [Fact] void should_complete_the_new_replay() => _error.ShouldBeNull();
    [Fact] void should_publish_the_rebuilt_state() => _primary.ShouldEqual(3);
    [Fact] void should_preserve_exactly_one_legacy_backup() => _preservedNames.Length.ShouldEqual(1);
    [Fact] void should_preserve_the_legacy_rows_after_pruning_the_old_name() => _preservedCount.ShouldEqual(1);
    [Fact] void should_keep_the_current_backup_after_pruning_the_old_name() => _currentBackupCount.ShouldEqual(2);
}
