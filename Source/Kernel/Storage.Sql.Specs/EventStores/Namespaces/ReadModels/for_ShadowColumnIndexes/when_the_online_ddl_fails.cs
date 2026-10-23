// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ShadowColumnIndexes;

public class when_the_online_ddl_fails : for_ReadModelMigrator.given.a_read_model_migrator
{
    Exception _failure;
    int? _timeout;
    int _ddlTimeout;

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"part":{"type":"string"}}}}}""");
        await using var plain = CreateContext("failed_ddl", ProjectedColumns.ForSchema(schema));
        var commands = new FailingDdl();
        var options = new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite(plain.Database.GetConnectionString()).AddInterceptors(commands).Options;
        await using var context = new ReadModelDbContext(options, plain.TableName, plain.Columns, _migrator);
        context.Database.SetCommandTimeout(19);
        _failure = await Catch.Exception(context.EnsureTableExists);
        _timeout = context.Database.GetCommandTimeout();
        _ddlTimeout = commands.Timeout;
    }

    [Fact] void should_not_hide_an_actual_ddl_failure() => _failure.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_restore_the_callers_timeout_even_after_a_failure() => _timeout.ShouldEqual(19);
    [Fact] void should_run_the_failed_statement_without_the_default_timeout() => _ddlTimeout.ShouldEqual(0);

    sealed class FailingDdl : DbCommandInterceptor
    {
        internal int Timeout { get; private set; }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.StartsWith("CREATE INDEX", StringComparison.Ordinal))
            {
                Timeout = command.CommandTimeout;
                throw new TimeoutException("Simulated provider failure");
            }
            return ValueTask.FromResult(result);
        }
    }
}
