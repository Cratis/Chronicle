// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Concepts.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_Sink.when_ending_a_replay;

[Collection(PostgreSqlCollection.Name)]
public class and_preserving_a_markerless_backup_rolls_back_on_postgresql(PostgreSqlFixture fixture) : given.a_read_model_with_a_markerless_backup<PostgreSqlSinkHarness>
{
    static readonly string _longName = new('x', 54);
    bool _failCommit;
    int? _legacyCount;
    int? _replayCount;
    int _recoveryTableCount;
    int _markerCount;

    protected override PostgreSqlSinkHarness CreateSqlHarness() => new() { Fixture = fixture, Interceptors = [new fail_commit(() => _failCommit)] };

    protected override ReadModelDefinition CreateReadModelDefinition() => base.CreateReadModelDefinition() with { ContainerName = _longName };

    void Establish() => _failCommit = true;

    async Task Because()
    {
        await PromoteNewReplay();
        _legacyCount = await StoredCount(_oldReplay.RevertContainerName.Value);
        _replayCount = await StoredCount($"replay-{_longName}");
        await using var context = OpenInspectionContext();
        _recoveryTableCount = await context.Database.SqlQuery<string>($"SELECT tablename AS \"Value\" FROM pg_tables WHERE schemaname = current_schema()")
            .CountAsync(name => name.StartsWith("chronicle_replay_backup_"));
        _markerCount = await context.ReplayPromotions.CountAsync();
    }

    [Fact] void should_report_the_failed_commit() => _error.ShouldBeOfExactType<commit_failed>();
    [Fact] void should_restore_the_legacy_backup_to_its_original_name() => _legacyCount.ShouldEqual(1);
    [Fact] void should_restore_the_original_primary() => _primary.ShouldEqual(2);
    [Fact] void should_preserve_the_rebuilt_rows_for_retry() => _replayCount.ShouldEqual(3);
    [Fact] void should_not_leave_a_preserved_copy_after_rollback() => _recoveryTableCount.ShouldEqual(0);
    [Fact] void should_not_publish_a_completion_marker() => _markerCount.ShouldEqual(0);

    sealed class fail_commit(Func<bool> armed) : DbTransactionInterceptor
    {
        public override ValueTask<InterceptionResult> TransactionCommittingAsync(DbTransaction transaction, TransactionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default)
        {
            if (armed())
            {
                throw new commit_failed();
            }
            return ValueTask.FromResult(result);
        }
    }

    sealed class commit_failed : Exception;
}
