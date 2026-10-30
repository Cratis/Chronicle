// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ShadowColumnIndexes;

public class when_the_online_ddl_fails : for_ReadModelMigrator.given.a_read_model_migrator
{
    readonly ILogger<ReadModelMigrator> _logger = Substitute.For<ILogger<ReadModelMigrator>>();
    readonly FailingDdl _commands = new();
    Exception _failure;
    int? _timeout;
    int _oldIndexes;
    int _indexes;
    string _warning = string.Empty;
    Exception _loggedFailure;

    async Task Because()
    {
        _logger.IsEnabled(LogLevel.Warning).Returns(true);
        _logger.WhenForAnyArgs(logger => logger.Log(default, default, default(object), default, default!)).Do(call =>
        {
            _warning = call[2]!.ToString()!;
            _loggedFailure = call[3] as Exception;
        });
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"part":{"type":"string"}}}}}""");
        await using var plain = CreateContext("failed_ddl", ProjectedColumns.ForSchema(schema));
        var options = new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite(plain.Database.GetConnectionString()).AddInterceptors(_commands).Options;
        var migrator = new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()), _logger);
        await using var context = new ReadModelDbContext(options, plain.TableName, plain.Columns, migrator);
        context.Database.SetCommandTimeout(19);
        _failure = await Catch.Exception(context.EnsureTableExists);
        _timeout = context.Database.GetCommandTimeout();
        await context.EnsureTableExists().WaitAsync(TimeSpan.FromSeconds(5));
        _oldIndexes = await context.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM pragma_index_list('failed_ddl') WHERE name={_commands.InvalidIndex}").SingleAsync();
        _indexes = await context.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM pragma_index_list('failed_ddl') WHERE name LIKE 'chronicle_shx_%'").SingleAsync();
    }

    [Fact] void should_not_hide_an_actual_ddl_failure() => _failure.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_restore_the_callers_timeout_even_after_a_failure() => _timeout.ShouldEqual(19);
    [Fact] void should_use_the_finite_default_timeout() => _commands.Timeout.ShouldEqual(600);
    [Fact] void should_pass_a_cancellable_token_to_the_command() => _commands.Cancellable.ShouldBeTrue();
    [Fact] void should_log_the_table_name() => _warning.ShouldContain("failed_ddl");
    [Fact] void should_log_the_index_name() => _warning.ShouldContain(_commands.InvalidIndex);
    [Fact] void should_log_the_failure() => _loggedFailure.ShouldEqual(_failure);
    [Fact] void should_release_the_reconciliation_gate_so_the_next_ensure_can_run() => _commands.Builds.ShouldEqual(2);
    [Fact] void should_drop_the_invalid_index_left_by_the_failed_build() => _oldIndexes.ShouldEqual(0);
    [Fact] void should_rebuild_one_usable_index() => _indexes.ShouldEqual(1);

    sealed class FailingDdl : DbCommandInterceptor
    {
        internal int Timeout { get; private set; }
        internal bool Cancellable { get; private set; }
        internal string InvalidIndex { get; private set; } = string.Empty;
        internal int Builds { get; private set; }

        public override async ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("CREATE INDEX", StringComparison.Ordinal) &&
                !command.CommandText.Contains("__initialized", StringComparison.Ordinal) && ++Builds == 1)
            {
                Timeout = command.CommandTimeout;
                Cancellable = cancellationToken.CanBeCanceled;
                InvalidIndex = command.CommandText.Split('"')[1];

                // SQLite cannot represent INVALID. Persist the failed build, then expose it as invalid in the catalog interceptor.
                await command.ExecuteNonQueryAsync(cancellationToken);
                throw new TimeoutException("Simulated provider timeout after creating an invalid index");
            }
            return result;
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (InvalidIndex.Length > 0 && command.CommandText.Contains("pragma_index_info", StringComparison.Ordinal))
            {
#pragma warning disable CA2100 // The index name comes from provider-delimited internal DDL, never user input.
                command.CommandText = command.CommandText.Replace("1 AS IsValid", $"i.name <> '{InvalidIndex}' AS IsValid", StringComparison.Ordinal);
#pragma warning restore CA2100
            }
            return ValueTask.FromResult(result);
        }
    }
}
