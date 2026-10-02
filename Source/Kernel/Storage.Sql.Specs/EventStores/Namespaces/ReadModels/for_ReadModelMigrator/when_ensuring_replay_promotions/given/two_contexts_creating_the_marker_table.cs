// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Arc.EntityFrameworkCore;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ReadModelMigrator.when_ensuring_replay_promotions.given;

public abstract class two_contexts_creating_the_marker_table<THarness> : Sinks.for_Sink.given.an_accumulating_sql_read_model<THarness>
    where THarness : ISqlSinkHarness, new()
{
    readonly TaskCompletionSource _bothCreating = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _firstCommitted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    protected Exception? _error;
    protected DbException? _duplicateError;
    protected int _createAttempts;
    protected int _markerCount = -1;

    protected async Task CreateConcurrently()
    {
        await using var first = OpenInspectionContext(ConnectionStringFor(true));
        await using var second = OpenInspectionContext(ConnectionStringFor(false));
        var firstMigrator = CreateMigrator(true);
        var secondMigrator = CreateMigrator(false);
        _error = await Catch.Exception(() => Task.WhenAll(
            firstMigrator.EnsureReplayPromotions(first),
            secondMigrator.EnsureReplayPromotions(second)));
        if (_error is null)
        {
            await using var inspection = OpenInspectionContext();
            _markerCount = await inspection.ReplayPromotions.CountAsync();
        }
    }

    string ConnectionStringFor(bool first)
    {
        // Distinct connection strings to the same database give each simulated silo its own
        // TableMigrator cache/lock. Both must pass the absent-table check before either CREATE.
        var builder = new DbConnectionStringBuilder { ConnectionString = _sqlHarness.ConnectionString };
        if (_sqlHarness.ConnectionString.GetDatabaseType() == DatabaseType.Sqlite)
        {
            builder["Default Timeout"] = first ? 7 : 9;
        }
        else
        {
            builder["Application Name"] = first ? "marker-race-first" : "marker-race-second";
        }
        return builder.ConnectionString;
    }

    ReadModelMigrator CreateMigrator(bool first)
    {
        var real = new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>());
        var interleaved = Substitute.For<ITableMigrator<ReadModelDbContext>>();
        interleaved.EnsureTableMigrated(Arg.Any<string>(), Arg.Any<ReadModelDbContext>(), Arg.Any<Func<ReadModelDbContext, string, Task>>(), Arg.Any<Func<ReadModelDbContext, string, Task>?>())
            .Returns(call => real.EnsureTableMigrated(call.ArgAt<string>(0), call.ArgAt<ReadModelDbContext>(1), call.ArgAt<Func<ReadModelDbContext, string, Task>>(2), call.ArgAt<Func<ReadModelDbContext, string, Task>?>(3)));
        interleaved.ExecuteMigrationOperations(Arg.Any<ReadModelDbContext>(), Arg.Any<MigrationBuilder>())
            .Returns(async call =>
            {
                if (Interlocked.Increment(ref _createAttempts) == 2)
                {
                    _bothCreating.TrySetResult();
                }
                await _bothCreating.Task.WaitAsync(TimeSpan.FromSeconds(10));
                if (!first)
                {
                    await _firstCommitted.Task.WaitAsync(TimeSpan.FromSeconds(10));
                }

                try
                {
                    await real.ExecuteMigrationOperations(call.ArgAt<ReadModelDbContext>(0), call.ArgAt<MigrationBuilder>(1));
                }
                catch (DbException error)
                {
                    _duplicateError = error;
                    throw;
                }
                if (first)
                {
                    _firstCommitted.TrySetResult();
                }
            });
        return new ReadModelMigrator(interleaved, Substitute.For<ILogger<ReadModelMigrator>>());
    }
}
