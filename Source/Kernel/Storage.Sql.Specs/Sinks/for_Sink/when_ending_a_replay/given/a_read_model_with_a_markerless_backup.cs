// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay.given;

public abstract class a_read_model_with_a_markerless_backup<THarness> : for_Sink.given.an_accumulating_sql_read_model<THarness>
    where THarness : ISqlSinkHarness, new()
{
    protected ReplayContext _oldReplay;
    protected ReplayContext _newReplay;
    protected Exception? _error;
    protected int? _primary;
    protected int _removedMarkers;
    protected int _remainingMarkers;
    protected bool _oldBackupExists;

    async Task Establish()
    {
        var name = CreateReadModelDefinition().ContainerName.Value;
        _oldReplay = ReplayContext() with { ContainerName = name, RevertContainerName = $"{name}-20260607110000" };
        _newReplay = ReplayContext() with { ContainerName = name, RevertContainerName = $"{name}-20260607120000-22222222" };
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await _sink.BeginReplay(_oldReplay);
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 42UL);
        await _sink.EndReplay(_oldReplay);

        // Leave the same tables and primary-key names as a pre-marker deployment.
        await using var context = OpenInspectionContext();
        _removedMarkers = await context.ReplayPromotions.ExecuteDeleteAsync();
    }

    protected async Task PromoteNewReplay()
    {
        await _sink.BeginReplay(_newReplay);
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(3), 43UL);
        _error = await Catch.Exception(() => _sink.EndReplay(_newReplay));
        _primary = await CurrentCountOrNull();
    }

    protected async Task PromoteAndPrune()
    {
        await PromoteNewReplay();
        await _sink.Remove(_oldReplay.RevertContainerName);
        _oldBackupExists = await TableExists(_oldReplay.RevertContainerName.Value);
        await using var context = OpenInspectionContext();
        _remainingMarkers = await context.ReplayPromotions.CountAsync();
    }
}
