// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Events.Constraints;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints.for_UniqueConstraintsStorage.given;

public abstract class batch_recovery_scenarios : a_provider_batch_storage
{
    readonly List<Action> _rollbackChecks = [];
    readonly List<Action> _recoveryChecks = [];

    protected async Task CheckRecovery()
    {
        foreach (var mode in new[] { UniqueConstraintMode.PerEventSource, UniqueConstraintMode.PerValue })
        {
            await CheckFailure(mode, new DbUpdateException("unrelated write failure"));
            await CheckFailure(mode, new InvalidOperationException("not a provider conflict"));
            await CheckFailure(mode, new OperationCanceledException("injected cancellation"));
            await CheckFailure(mode, null);
            await CheckCanceledConflict(mode);
            foreach (var conflict in transaction_conflicts.All()) await CheckConflict(mode, conflict);
            await CheckWinner(mode);
        }
    }

    protected void VerifyRollback()
    {
        foreach (var check in _rollbackChecks) check();
    }

    protected void VerifyRecovery()
    {
        foreach (var check in _recoveryChecks) check();
        VerifyChanges();
    }

    static UniqueConstraintIndexChange[] ChangesAfterDelete() =>
        [Remove("released"), Save("owner", "new", 99UL, subject: "updated"), Save("new-source", "new-value"), Save("loser", "held")];

    async Task CheckFailure(UniqueConstraintMode mode, Exception? failure)
    {
        await Prepare(mode);
        await Seed();
        var before = await Snapshot();
        var interceptor = new a_late_batch_failure(failure);
        var deleted = new a_completed_batch_delete();
        _interceptors.AddRange([interceptor, deleted]);
        using var counter = new batch_collision_counter();
        var actual = await Catch.Exception(() => _storage.ApplyChanges(_definition, ChangesAfterDelete()));
        _interceptors.Clear();
        var disposed = _batchContexts.Count == _disposedBatchContexts;
        var after = await Snapshot();
        _rollbackChecks.Add(() =>
        {
            actual.ShouldEqual(failure ?? interceptor.Failure);
            actual.ShouldNotBeNull();
            interceptor.Attempts.ShouldEqual(1);
            interceptor.SuccessfulChunks.ShouldEqual(0);
            deleted.DeletedRows.ShouldBeGreaterThan(0);
            after.ShouldEqual(before);
            disposed.ShouldBeTrue();
            counter.Count.ShouldEqual(0);
        });
    }

    async Task CheckConflict(UniqueConstraintMode mode, Exception failure)
    {
        await Prepare(mode);
        await Seed();
        var interceptor = new a_late_batch_failure(failure);
        var deleted = new a_completed_batch_delete();
        _interceptors.AddRange([interceptor, deleted]);
        using var counter = new batch_collision_counter();
        await CheckChanges(ChangesAfterDelete());
        _interceptors.Clear();
        var disposed = _batchContexts.Count == _disposedBatchContexts;
        _recoveryChecks.Add(() =>
        {
            interceptor.Attempts.ShouldBeGreaterThan(1);
            deleted.DeletedRows.ShouldBeGreaterThan(0);
            disposed.ShouldBeTrue();
            counter.Count.ShouldEqual(0);
        });
    }

    async Task CheckCanceledConflict(UniqueConstraintMode mode)
    {
        await Prepare(mode);
        await Seed();
        var before = await Snapshot();
        using var cancellation = new CancellationTokenSource();
        var interceptor = new a_late_batch_failure(new SqliteException("busy", 5), cancellation.Cancel);
        var deleted = new a_completed_batch_delete();
        _interceptors.AddRange([interceptor, deleted]);
        using var counter = new batch_collision_counter();
        var actual = await Catch.Exception(() => _storage.ApplyChanges(_definition, ChangesAfterDelete(), cancellation.Token));
        _interceptors.Clear();
        var after = await Snapshot();
        _rollbackChecks.Add(() =>
        {
            actual.ShouldBeOfExactType<OperationCanceledException>();
            interceptor.Attempts.ShouldEqual(1);
            interceptor.SuccessfulChunks.ShouldEqual(0);
            deleted.DeletedRows.ShouldBeGreaterThan(0);
            after.ShouldEqual(before);
            counter.Count.ShouldEqual(0);
        });
    }

    async Task CheckWinner(UniqueConstraintMode mode)
    {
        await Prepare(mode);
        await _oracle.Save("winner", _definition, 42UL, "contended", subject: "winner-metadata");
        var interceptor = new a_forced_unique_failure(() => _storage.Save("winner", _definition, 42UL, "contended", subject: "winner-metadata"));
        _interceptors.Add(interceptor);
        using var counter = new batch_collision_counter();
        await CheckChanges([Save("loser", "contended", 43UL, subject: "loser-metadata"), Save("allowed", "free", 44UL, subject: "allowed-metadata")]);
        _interceptors.Clear();
        var snapshot = await Snapshot();
        _recoveryChecks.Add(() =>
        {
            interceptor.UniqueFailures.ShouldEqual(1);
            counter.Count.ShouldEqual(1);
            snapshot.Length.ShouldEqual(2);
            snapshot.Any(row => row.EndsWith(":winner:contended:42:winner-metadata", StringComparison.Ordinal)).ShouldBeTrue();
            snapshot.Any(row => row.EndsWith(":allowed:free:44:allowed-metadata", StringComparison.Ordinal)).ShouldBeTrue();
        });
    }
}
