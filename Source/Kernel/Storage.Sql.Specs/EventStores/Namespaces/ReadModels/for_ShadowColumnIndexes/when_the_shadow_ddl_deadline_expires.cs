// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ShadowColumnIndexes;

public class when_the_shadow_ddl_deadline_expires : for_ReadModelMigrator.given.a_read_model_migrator
{
    readonly ILogger<ReadModelMigrator> _logger = Substitute.For<ILogger<ReadModelMigrator>>();
    readonly BlockedDdl _commands = new();
    readonly given.DdlClock _clock = new();
    Exception _failure;
    Exception _retryFailure;
    Exception _loggedFailure;
    string _warning = string.Empty;
    int? _restoredTimeout;

    async Task Because()
    {
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        _logger.WhenForAnyArgs(logger => logger.Log(default, default, default(object), default, default!)).Do(call =>
        {
            _warning = call[2]!.ToString()!;
            _loggedFailure = call[3] as Exception;
        });
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"part":{"type":"string"}}}}}""");
        await using var plain = CreateContext("ddl_deadline", ProjectedColumns.ForSchema(schema));
        var options = new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite(plain.Database.GetConnectionString()).AddInterceptors(_commands).Options;
        var configuration = Options.Create(new ChronicleOptions { Sql = new() { ShadowIndexDdlTimeoutSeconds = 1 } });
        var migrator = new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()), _logger, configuration, _clock);
        await using var context = new ReadModelDbContext(options, plain.TableName, plain.Columns, migrator);
        context.Database.SetCommandTimeout(19);
        var migration = Catch.Exception(context.EnsureTableExists);
        await _commands.Blocked.Task;
        _clock.Advance(TimeSpan.FromSeconds(1));
        _failure = await migration;
        _restoredTimeout = context.Database.GetCommandTimeout();
        _retryFailure = await Catch.Exception(context.EnsureTableExists);
    }

    [Fact] void should_surface_cancellation_to_the_migration_caller() => (_failure is OperationCanceledException).ShouldBeTrue();
    [Fact] void should_cancel_the_command_even_without_a_host_token() => _commands.Cancelled.ShouldBeTrue();
    [Fact] void should_use_the_same_finite_command_timeout() => _commands.Timeout.ShouldEqual(1);
    [Fact] void should_restore_the_callers_timeout() => _restoredTimeout.ShouldEqual(19);
    [Fact] void should_log_the_table_name() => _warning.ShouldContain("ddl_deadline");
    [Fact] void should_log_the_index_name() => _warning.ShouldContain(_commands.IndexName);
    [Fact] void should_log_the_cancellation_exception() => _loggedFailure.ShouldEqual(_failure);
    [Fact] void should_release_the_gate_and_allow_the_next_ensure() => _retryFailure.ShouldBeNull();
    [Fact] void should_retry_the_failed_ddl_instead_of_caching_success() => _commands.Attempts.ShouldEqual(2);

    sealed class BlockedDdl : DbCommandInterceptor
    {
        internal TaskCompletionSource Blocked { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal int Attempts { get; private set; }
        internal int Timeout { get; private set; }
        internal bool Cancelled { get; private set; }
        internal string IndexName { get; private set; } = string.Empty;

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("CREATE INDEX", StringComparison.Ordinal) && ++Attempts == 1)
            {
                Timeout = command.CommandTimeout;
                IndexName = command.CommandText.Split('"')[1];
                var cancelled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                await using var registration = cancellationToken.Register(() => cancelled.TrySetCanceled(cancellationToken));
                Blocked.TrySetResult();
                try
                {
                    await cancelled.Task;
                }
                finally
                {
                    Cancelled = cancellationToken.IsCancellationRequested;
                }
            }
            return result;
        }
    }
}
