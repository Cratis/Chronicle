// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Storage.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_binding_long_table_names.given;

public abstract class a_long_replayed_read_model<THarness> : for_Sink.given.an_accumulating_sql_read_model<THarness>
    where THarness : ISqlSinkHarness, new()
{
    protected static readonly string _logical = new('x', 140);
    protected string _liveName;
    protected string _replayName;
    protected string _firstBackupName;
    protected string _secondBackupName;
    protected int _current;

    private protected abstract IReadModelTables Tables { get; }

    protected override ReadModelDefinition CreateReadModelDefinition() => base.CreateReadModelDefinition() with { ContainerName = _logical };

    protected async Task PromoteTwice()
    {
        var first = ReplayContext() with { ContainerName = _logical, RevertContainerName = $"{_logical}-20261101010000-first" };
        var second = first with { RevertContainerName = $"{_logical}-20261101020000-second" };
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(1), 41UL);
        await Replay(first, 2, 42UL);
        await Replay(second, 3, 43UL);
        _current = await CurrentCount();
        _liveName = await PhysicalName(ReadModelTableRole.Live, _logical);
        _replayName = await PhysicalName(ReadModelTableRole.Replay, ReadModelTableNames.Replay(_logical));
        _firstBackupName = await PhysicalName(ReadModelTableRole.Backup, first.RevertContainerName.Value);
        _secondBackupName = await PhysicalName(ReadModelTableRole.Backup, second.RevertContainerName.Value);
    }

    async Task Replay(ReplayContext context, int count, ulong sequenceNumber)
    {
        await _sink.BeginReplay(context);
        await _sink.ApplyChanges(_key, ChangesetSettingCountTo(count), sequenceNumber);
        await _sink.EndReplay(context);
    }

    async Task<string> PhysicalName(ReadModelTableRole role, string logical)
    {
        var handle = ReadModelTableHandle.For("test-event-store", "test-namespace", "test-read-model", _logical, role, logical);
        return (await Tables.Find(handle))!.PhysicalName;
    }
}
