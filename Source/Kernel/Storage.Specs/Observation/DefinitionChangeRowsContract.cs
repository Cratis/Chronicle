// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Orleans.Jobs;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Exercises the same row contract, including absorbing completion, on each real provider.
/// </summary>
public abstract class DefinitionChangeRowsContract : Specification
{
    readonly ObserverKey _observer = new("observer", "store", "namespace", "event-log");
    readonly List<string> _failures = [];

    /// <summary>
    /// Opens a new store over the same provider database, or the same in-memory store.
    /// </summary>
    /// <typeparam name="T">The row payload.</typeparam>
    /// <returns>The family store.</returns>
    protected abstract IObserverDefinitionChangeRows<T> OpenRows<T>();

    async Task Because()
    {
        JobId job = Guid.NewGuid();
        var owner = new FailedPartitionRecoveryOwner(Guid.NewGuid(), job.Value, 3, FailedPartitionRecoveryState.Resuming, 1);
        var failure = new FailedPartition
        {
            ObserverId = "observer",
            Partition = "partition",
            RecoveryCursor = new(new(71), FailedPartitionRecoveryMode.Replay, new(100)),
            RecoveryOwner = owner,
            Hold = new(FailedPartitionHoldReason.WaitingForHandler, new("event-a"), DateTimeOffset.UtcNow),
            Attempts = [new() { SequenceNumber = new(70), Kind = FailureKind.Handling }],
            IsQuarantined = true
        };
        await Check(new ObserverProgressRecord(job, "partition", ObserverWorkOutcome.Checkpoint, DateTimeOffset.UtcNow, owner),
            new ObserverProgressRecord(job, "partition", ObserverWorkOutcome.Held, DateTimeOffset.UtcNow, owner));
        await Check(new ObserverContinuation("partition", job), new ObserverContinuation("partition", job));
        await Check(new ObserverRestartPlan(job, "partition", new(1), new(100), ObserverReaderScope.Explicit([new("event-a", new(2))])),
            new ObserverRestartPlan(job, null, new(1), new(100), ObserverReaderScope.Wildcard));
        await Check(new ObserverJobInventory(job, "CatchUpObserver", ObserverDefinitionChangeJobKind.CatchUp, new(false, "continuation")),
            new ObserverJobInventory(job, "CatchUpObserver", ObserverDefinitionChangeJobKind.CatchUp, new(false, "continuation"), new(ObserverWorkOutcome.Held, new(70), new(71), false, failure)));
    }

    [Fact] void should_round_trip_every_family() => _failures.Where(failure => failure.StartsWith("roundtrip", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_enforce_conditional_forward_only_updates() => _failures.Where(failure => failure.StartsWith("conditional", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_isolate_and_page_without_losing_rows() => _failures.Where(failure => failure.StartsWith("paging", StringComparison.Ordinal)).ShouldBeEmpty();
    [Fact] void should_require_the_consumed_version_for_deletion() => _failures.Where(failure => failure.StartsWith("delete", StringComparison.Ordinal)).ShouldBeEmpty();

    async Task Check<T>(T initial, T terminal)
    {
        var rows = OpenRows<T>();
        var row = new ObserverDefinitionChangeRow<T>(new(_observer, new(1), "row"), 0, ObserverDefinitionChangeRowState.Intended, initial, new(10));
        Record("roundtrip missing", await rows.Get(row.Key) is null);
        Record("paging empty legacy", (await rows.ReadPage(_observer, null, null, 10)).Rows.Count == 0);
        Record("conditional insert", await rows.TryInsert(row));
        Record("conditional duplicate", !await OpenRows<T>().TryInsert(row));
        var saved = await OpenRows<T>().Get(row.Key);
        Record("roundtrip snapshot", ObserverDefinitionChangeRows.Serialize(saved!) == ObserverDefinitionChangeRows.Serialize(row));
        var started = row with { Version = 1, State = ObserverDefinitionChangeRowState.Started, Position = new(20) };
        var race = await Task.WhenAll(OpenRows<T>().TryReplace(started, 0), OpenRows<T>().TryReplace(started, 0));
        Record("conditional one winner", race.Count(success => success) == 1);
        Record("conditional stale", !await rows.TryReplace(started, 0));
        Record("conditional backwards state", !await rows.TryReplace(started with { Version = 2, State = ObserverDefinitionChangeRowState.Intended }, 1));
        Record("conditional backwards position", !await rows.TryReplace(started with { Version = 2, Position = new(19) }, 1));
        Record("conditional lost position", !await rows.TryReplace(started with { Version = 2, Position = null }, 1));
        var ended = started with { Version = 2, State = ObserverDefinitionChangeRowState.Terminal, Data = terminal };
        Record("conditional terminal", await rows.TryReplace(ended, 1));
        Record("conditional late start", !await rows.TryReplace(started with { Version = 3 }, 2));
        Record("conditional absorbing", !await rows.TryReplace(ended with { Version = 3 }, 2));
        Record("roundtrip terminal", ObserverDefinitionChangeRows.Serialize((await OpenRows<T>().Get(row.Key))!) == ObserverDefinitionChangeRows.Serialize(ended));
        var immediate = row with { Key = row.Key with { Observer = _observer with { ObserverId = "empty-replay" } } };
        Record("conditional immediate insert", await rows.TryInsert(immediate));
        Record("conditional completion inside start", await rows.TryReplace(immediate with { Version = 1, State = ObserverDefinitionChangeRowState.Terminal, Data = terminal }, 0));
        Record("conditional delayed start answer", !await rows.TryReplace(immediate with { Version = 2, State = ObserverDefinitionChangeRowState.Started }, 1));
        await CheckPages(rows, row);
        Record("delete stale", !await rows.TryDelete(row.Key, 1));
        Record("delete current", await rows.TryDelete(row.Key, 2));
        Record("delete absent", await OpenRows<T>().Get(row.Key) is null);

        void Record(string check, bool passed)
        {
            if (!passed) _failures.Add($"{check}: {typeof(T).Name}");
        }
    }

    async Task CheckPages<T>(IObserverDefinitionChangeRows<T> rows, ObserverDefinitionChangeRow<T> row)
    {
        var siblings = new[]
        {
            row with { Key = row.Key with { Id = "second" } },
            row with { Key = row.Key with { Revision = new(2) } },
            row with { Key = row.Key with { Observer = _observer with { ObserverId = "other" } } },
            row with { Key = row.Key with { Observer = _observer with { EventStore = "other" } } },
            row with { Key = row.Key with { Observer = _observer with { Namespace = "other" } } },
            row with { Key = row.Key with { Observer = _observer with { EventSequenceId = "other" } } }
        };
        foreach (var sibling in siblings) (await rows.TryInsert(sibling)).ShouldBeTrue();
        var first = await rows.ReadPage(_observer, new(1), null, 1);
        (await rows.TryDelete(first.Rows.Single().Key, first.Rows.Single().Version)).ShouldBeTrue();
        var second = await rows.ReadPage(_observer, new(1), first.Next, 1);
        var end = await rows.ReadPage(_observer, new(1), second.Next, 1);
        if (first.Rows.Count != 1 || second.Rows.Count != 1 || first.Next == second.Next || end.Rows.Count != 0)
            _failures.Add($"paging keyset after deletion: {typeof(T).Name}");
        var all = await rows.ReadPage(_observer, null, null, int.MaxValue);
        if (all.Rows.Count != 2 || (await rows.ReadPage(_observer, new(2), null, 10)).Rows.Count != 1 || (await rows.ReadPage(_observer, null, null, 0)).Rows.Count != 0)
            _failures.Add($"paging isolation and all-revision harvest: {typeof(T).Name}");

        // Leave the original terminal row for the version-conditional deletion checks.
        if (first.Rows.Single().Key == row.Key)
        {
            (await rows.TryInsert(row with { Version = 0 })).ShouldBeTrue();
            (await rows.TryReplace(row with { Version = 1, State = ObserverDefinitionChangeRowState.Started, Position = new(20) }, 0)).ShouldBeTrue();
            (await rows.TryReplace(first.Rows.Single(), 1)).ShouldBeTrue();
        }
    }
}
