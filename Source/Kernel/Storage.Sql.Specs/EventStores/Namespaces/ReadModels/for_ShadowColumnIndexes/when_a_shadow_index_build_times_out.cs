// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ShadowColumnIndexes;

public class when_a_shadow_index_build_times_out : for_ReadModelMigrator.given.a_read_model_migrator
{
    readonly Commands _commands = new();
    Exception _failure;
    Exception _retryFailure;
    int _indexes;
    int? _restoredTimeout;

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"part":{"type":"string"}}}}}""");
        await using var plain = CreateContext("index_timeout", ProjectedColumns.ForSchema(schema));
        var options = new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite(plain.Database.GetConnectionString()).AddInterceptors(_commands).Options;
        await using var context = new ReadModelDbContext(options, plain.TableName, plain.Columns, _migrator);
        _failure = await Catch.Exception(context.EnsureTableExists);
        _retryFailure = await Catch.Exception(context.EnsureTableExists);
        _restoredTimeout = context.Database.GetCommandTimeout();
        _indexes = await context.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM pragma_index_list('index_timeout') WHERE name LIKE 'chronicle_shx_%'").SingleAsync();
    }

    [Fact] void should_not_use_the_default_timeout_for_a_long_index_build() => _failure.ShouldBeNull();
    [Fact] void should_not_repeat_a_timeout_on_the_next_ensure() => _retryFailure.ShouldBeNull();
    [Fact] void should_build_the_index_only_once() => _commands.Attempts.ShouldEqual(1);
    [Fact] void should_install_the_index() => _indexes.ShouldEqual(1);
    [Fact] void should_restore_the_callers_timeout() => _restoredTimeout.ShouldBeNull();
    [Fact] void should_leave_catalog_and_ordinary_queries_on_the_default_timeout() => _commands.QueryTimeouts.ShouldEachConformTo(timeout => timeout == 30);

    sealed class Commands : DbCommandInterceptor
    {
        internal int Attempts { get; private set; }
        internal List<int> QueryTimeouts { get; } = [];

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("CREATE INDEX", StringComparison.Ordinal) &&
                !command.CommandText.Contains("__initialized", StringComparison.Ordinal))
            {
                Attempts++;
                if (command.CommandTimeout != 600)
                {
                    throw new TimeoutException("The shadow index must use the finite ten-minute timeout");
                }
            }
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            QueryTimeouts.Add(command.CommandTimeout);
            return ValueTask.FromResult(result);
        }
    }
}
