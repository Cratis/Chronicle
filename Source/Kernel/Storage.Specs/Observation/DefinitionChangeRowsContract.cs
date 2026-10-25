// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Provider-independent row behaviors. Each fact exercises one contract, on fresh provider storage.
/// </summary>
// Each fact writes different rows into the same provider storage and needs a fresh database.
[PerFactSpecificationLifecycle]
public abstract partial class DefinitionChangeRowsContract : Specification
{
    readonly ObserverKey _observer = new("observer", "store", "namespace", "event-log");
    readonly JobId _job = Guid.NewGuid();

    /// <summary>
    /// Opens a new store over the same provider database, or the same in-memory store.
    /// </summary>
    /// <typeparam name="T">The row payload.</typeparam>
    /// <returns>The family store.</returns>
    protected abstract IObserverDefinitionChangeRows<T> OpenRows<T>();

    [Fact] public async Task should_round_trip_progress() => await RoundTrip(Progress());
    [Fact] public async Task should_round_trip_continuations() => await RoundTrip(Continuation());
    [Fact] public async Task should_round_trip_restart_plans() => await RoundTrip(Plan());
    [Fact] public async Task should_round_trip_inventory() => await RoundTrip(Inventory());
    [Fact] public async Task should_read_a_missing_row_as_absent() => (await OpenRows<ObserverContinuation>().Get(Continuation().Key)).ShouldBeNull();
    [Fact] public async Task should_reject_duplicate_insertions()
    {
        var row = Continuation();
        (await OpenRows<ObserverContinuation>().TryInsert(row)).ShouldBeTrue();
        (await OpenRows<ObserverContinuation>().TryInsert(row)).ShouldBeFalse();
    }
    [Fact] public async Task should_choose_exactly_one_concurrent_replacement()
    {
        var row = Continuation();
        await Insert(row);
        var started = row with { Version = 1, State = ObserverDefinitionChangeRowState.Started };
        var results = await Task.WhenAll(OpenRows<ObserverContinuation>().TryReplace(started, 0), OpenRows<ObserverContinuation>().TryReplace(started, 0));
        results.Count(success => success).ShouldEqual(1);
    }
    [Fact] public async Task should_reject_stale_replacements()
    {
        var row = Continuation();
        await Insert(row);
        (await OpenRows<ObserverContinuation>().TryReplace(row with { Version = 1 }, 0)).ShouldBeTrue();
        (await OpenRows<ObserverContinuation>().TryReplace(row with { Version = 1 }, 0)).ShouldBeFalse();
    }
    [Fact] public async Task should_not_move_state_backwards()
    {
        var row = Continuation() with { State = ObserverDefinitionChangeRowState.Started };
        await Insert(row);
        (await OpenRows<ObserverContinuation>().TryReplace(row with { Version = 1, State = ObserverDefinitionChangeRowState.Intended }, 0)).ShouldBeFalse();
    }
    [Fact] public async Task should_not_move_position_backwards()
    {
        var row = Continuation();
        await Insert(row);
        (await OpenRows<ObserverContinuation>().TryReplace(row with { Version = 1, Position = new(9) }, 0)).ShouldBeFalse();
    }
    [Fact] public async Task should_not_erase_a_position()
    {
        var row = Continuation();
        await Insert(row);
        (await OpenRows<ObserverContinuation>().TryReplace(row with { Version = 1, Position = null }, 0)).ShouldBeFalse();
    }
    [Fact] public async Task should_preserve_unsigned_positions_above_int64()
    {
        var row = Continuation() with { Position = new(18446744073709551613UL) };
        await Insert(row);
        var next = row with { Version = 1, Position = new(18446744073709551614UL) };
        (await OpenRows<ObserverContinuation>().TryReplace(next, 0)).ShouldBeTrue();
        (await OpenRows<ObserverContinuation>().TryReplace(row with { Version = 2 }, 1)).ShouldBeFalse();
        (await OpenRows<ObserverContinuation>().Get(row.Key))!.Position.ShouldEqual(next.Position);
    }
    [Fact] public async Task should_require_the_consumed_version_for_deletion()
    {
        var row = Continuation();
        await Insert(row);
        (await OpenRows<ObserverContinuation>().TryDelete(row.Key, 1)).ShouldBeFalse();
        (await OpenRows<ObserverContinuation>().Get(row.Key)).ShouldNotBeNull();
    }
    [Fact] public async Task should_delete_the_consumed_row()
    {
        var row = Continuation();
        await Insert(row);
        (await OpenRows<ObserverContinuation>().TryDelete(row.Key, 0)).ShouldBeTrue();
        (await OpenRows<ObserverContinuation>().Get(row.Key)).ShouldBeNull();
    }

    ObserverDefinitionChangeRow<T> Row<T>(T data, ObserverDefinitionChangeRowState state = ObserverDefinitionChangeRowState.Intended) => new(new(_observer, new(1), "row"), 0, state, data, new(10));
    ObserverDefinitionChangeRow<ObserverProgressRecord> Progress() => Row(new ObserverProgressRecord(_job, "partition", ObserverWorkOutcome.Checkpoint, DateTimeOffset.UtcNow), ObserverDefinitionChangeRowState.Started);
    ObserverDefinitionChangeRow<ObserverContinuation> Continuation() => Row(new ObserverContinuation("partition", _job));
    ObserverDefinitionChangeRow<ObserverRestartPlan> Plan() => Row(new ObserverRestartPlan(_job, "partition", new(1), new(100), ObserverReaderScope.Explicit([new("event-a", new(2))])));
    ObserverDefinitionChangeRow<ObserverJobInventory> Inventory() => Row(new ObserverJobInventory(_job, "CatchUpObserver", ObserverDefinitionChangeJobKind.CatchUp, new(false, "continuation")));

    async Task Insert<T>(ObserverDefinitionChangeRow<T> row) => (await OpenRows<T>().TryInsert(row)).ShouldBeTrue();

    async Task RoundTrip<T>(ObserverDefinitionChangeRow<T> row)
    {
        await Insert(row);
        ObserverDefinitionChangeRows.Serialize((await OpenRows<T>().Get(row.Key))!).ShouldEqual(ObserverDefinitionChangeRows.Serialize(row));
    }
}
