// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

[Collection(PostgreSqlCollection.Name)]
public class and_a_truncated_markerless_half_swap_has_no_primary_on_postgresql(PostgreSqlFixture fixture) : for_Sink.given.an_accumulating_sql_read_model<PostgreSqlSinkHarness>
{
    static readonly string _longName = new('x', 54);
    readonly ReplayContext _replay = ReplayContext() with { ContainerName = _longName, RevertContainerName = $"{_longName}-20260607120000-22222222" };
    Exception? _error;
    bool _primaryExists;
    int? _backupCount;
    int? _replayCount;
    int _recoveryTableCount;
    int _markerCount;

    protected override PostgreSqlSinkHarness CreateSqlHarness() => new() { Fixture = fixture };

    protected override ReadModelDefinition CreateReadModelDefinition() => base.CreateReadModelDefinition() with { ContainerName = _longName };

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await _sink.BeginReplay(_replay);
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 42UL);

        // The old two-rename implementation committed this first rename independently.
        await using var context = OpenInspectionContext();
        var helper = context.GetService<ISqlGenerationHelper>();
#pragma warning disable EF1002 // Specification-owned identifiers delimited by the provider.
        await context.Database.ExecuteSqlRawAsync($"ALTER TABLE {helper.DelimitIdentifier(_longName)} RENAME TO {helper.DelimitIdentifier(_replay.RevertContainerName.Value)}");
#pragma warning restore EF1002
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _sink.EndReplay(_replay));
        _primaryExists = await TableExists(_longName);
        _backupCount = await StoredCount(_replay.RevertContainerName.Value);
        _replayCount = await StoredCount($"replay-{_longName}");
        await using var context = OpenInspectionContext();
        _recoveryTableCount = await context.Database.SqlQuery<string>($"SELECT tablename AS \"Value\" FROM pg_tables WHERE schemaname = current_schema()")
            .CountAsync(name => name.StartsWith("chronicle_replay_backup_"));
        _markerCount = await context.ReplayPromotions.CountAsync();
    }

    [Fact] void should_report_the_unverified_half_swap() => _error.ShouldBeOfExactType<UnverifiedReplayBackup>();
    [Fact] void should_not_recreate_the_missing_primary() => _primaryExists.ShouldBeFalse();
    [Fact] void should_preserve_the_previous_state_at_the_revert_name() => _backupCount.ShouldEqual(1);
    [Fact] void should_preserve_the_rebuilt_rows_for_recovery() => _replayCount.ShouldEqual(2);
    [Fact] void should_not_rename_the_revert_table_aside() => _recoveryTableCount.ShouldEqual(0);
    [Fact] void should_not_record_a_completed_promotion() => _markerCount.ShouldEqual(0);
}
