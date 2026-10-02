// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Npgsql;

using Contract = Cratis.Chronicle.Storage.Sinks.for_ISink.given;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

[Collection(PostgreSqlCollection.Name)]
public class and_two_revert_names_are_truncated_to_the_same_table_on_postgresql(PostgreSqlFixture fixture) : Contract.an_accumulating_read_model<PostgreSqlSinkHarness>
{
    static readonly string _longName = new('x', 54);
    readonly ReplayContext _first = ReplayContext() with { ContainerName = _longName, RevertContainerName = $"{_longName}-20260607123456-11111111" };
    readonly ReplayContext _second = ReplayContext() with { ContainerName = _longName, RevertContainerName = $"{_longName}-20260607123456-22222222" };
    PostgreSqlSinkHarness _sqlHarness;
    Exception? _pruningError;
    Exception? _backupReadError;
    int? _primary;
    int? _backup;
    int _markerCount;
    string? _markerRevertName;

    protected override PostgreSqlSinkHarness CreateHarness() => _sqlHarness = new() { Fixture = fixture };

    protected override ReadModelDefinition CreateReadModelDefinition() => base.CreateReadModelDefinition() with { ContainerName = _longName };

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await _sink.BeginReplay(_first);
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 42UL);
        await _sink.EndReplay(_first);
    }

    async Task Because()
    {
        await _sink.BeginReplay(_second);
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(3), 43UL);
        await _sink.EndReplay(_second);
        _primary = await CurrentCountOrNull();
        _pruningError = await Catch.Exception(() => _sink.Remove(_first.RevertContainerName));

        // Read the physical backup without the migrator creating or altering a logical container.
        await using var connection = new NpgsqlConnection(_sqlHarness.ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
#pragma warning disable CA2100 // Specification-owned identifier containing only x, digits, and a hyphen.
        command.CommandText = $"SELECT count FROM \"{_first.RevertContainerName.Value[..63]}\"";
#pragma warning restore CA2100
        _backupReadError = await Catch.Exception(async () =>
            _backup = Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture));
        command.CommandText = "SELECT COUNT(*) FROM chronicle_replay_promotions";
        _markerCount = Convert.ToInt32(await command.ExecuteScalarAsync(), CultureInfo.InvariantCulture);
        command.CommandText = "SELECT \"RevertContainerName\" FROM chronicle_replay_promotions";
        _markerRevertName = (string?)await command.ExecuteScalarAsync();
    }

    [Fact] void should_publish_the_second_replay() => _primary.ShouldEqual(3);
    [Fact] void should_prune_the_old_logical_backup_without_error() => _pruningError.ShouldBeNull();
    [Fact] void should_keep_the_current_backup_table() => _backupReadError.ShouldBeNull();
    [Fact] void should_preserve_the_current_backup_after_pruning_the_old_name() => _backup.ShouldEqual(2);
    [Fact] void should_remove_the_pruned_replays_marker() => _markerCount.ShouldEqual(1);
    [Fact] void should_keep_the_current_replays_full_untruncated_identity() => _markerRevertName.ShouldEqual(_second.RevertContainerName.Value);
}
