// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_removing.given;

public abstract class a_replayed_read_model_with_a_changed_container<THarness> : for_Sink.given.an_accumulating_sql_read_model<THarness>
    where THarness : ISqlSinkHarness, new()
{
    protected bool _backupExists;
    protected bool _markerExists;
    protected int? _currentCount;

    async Task Establish()
    {
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await _sink.BeginReplay(ReplayContext());
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(2), 42UL);
        await _sink.EndReplay(ReplayContext());

        // UpdateReadModelDefinition changes the container used by a newly resolved sink, not the
        // logical revert name already stored in the retained replay occurrence.
        _sink = _sqlHarness.CreateSink(CreateReadModelDefinition() with { ContainerName = "renamed_read_model" });
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(7), 43UL);
    }

    protected async Task PruneOldReplay()
    {
        await _sink.Remove(ReplayContext().RevertContainerName);
        _backupExists = await TableExists(ReplayContext().RevertContainerName.Value);
        await using var context = OpenInspectionContext();
        _markerExists = await context.ReplayPromotions.AnyAsync();
        _currentCount = await CurrentCountOrNull();
    }
}
