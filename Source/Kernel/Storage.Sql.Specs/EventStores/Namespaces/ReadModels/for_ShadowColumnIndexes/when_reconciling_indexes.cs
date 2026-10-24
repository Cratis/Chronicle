// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Concepts.ReadModels;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ShadowColumnIndexes;

public class when_reconciling_indexes : for_ReadModelMigrator.given.a_read_model_migrator
{
    readonly Commands _commands = new();
    int _repairedIndexes;

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"string"},"amount":{"type":"number","format":"decimal"}}}""");
        await using var context = CreateContext("shadow_indexes", ProjectedColumns.ForSchema(schema, [new IndexDefinition("amount")]));
        var options = new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite(context.Database.GetConnectionString()).AddInterceptors(_commands).Options;
        await using var indexed = new ReadModelDbContext(options, context.TableName, context.Columns, _migrator);
        await indexed.EnsureTableExists();
        await using var unindexed = new ReadModelDbContext(options, context.TableName, ProjectedColumns.ForSchema(schema), _migrator);
        await unindexed.EnsureTableExists();
        await unindexed.Database.ExecuteSqlRawAsync("ALTER TABLE shadow_indexes DROP COLUMN __order_amount");
        await ShadowColumns.Access(indexed, "__order_amount", () => indexed.Database.SqlQuery<long>($"SELECT COUNT(__order_amount) AS Value FROM shadow_indexes").SingleAsync());
        _repairedIndexes = await indexed.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM pragma_index_list('shadow_indexes') WHERE name LIKE 'chronicle_shx_%'").SingleAsync();
    }

    [Fact] void should_create_the_shadow_index_idempotently() => _commands.Sql.Exists(sql => sql.StartsWith("CREATE INDEX IF NOT EXISTS \"chronicle_shx_", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_drop_the_obsolete_index_idempotently() => _commands.Sql.Exists(sql => sql.StartsWith("DROP INDEX IF EXISTS \"chronicle_shx_", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_sqlite_ddl_plain() => _commands.Sql.Exists(sql => sql.Contains("CONCURRENTLY", StringComparison.Ordinal) || sql.Contains("ONLINE", StringComparison.Ordinal)).ShouldBeFalse();
    [Fact] void should_execute_index_ddl_outside_a_transaction() => _commands.TransactionalIndex.ShouldBeFalse();
    [Fact] void should_use_the_same_index_creation_path_when_self_healing() => _repairedIndexes.ShouldEqual(1);

    sealed class Commands : DbCommandInterceptor
    {
        internal List<string> Sql { get; } = [];
        internal bool TransactionalIndex { get; private set; }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Sql.Add(command.CommandText);
            if (command.CommandText.StartsWith("CREATE INDEX", StringComparison.Ordinal) || command.CommandText.StartsWith("DROP INDEX", StringComparison.Ordinal))
            {
                TransactionalIndex |= command.Transaction is not null;
            }
            return ValueTask.FromResult(result);
        }
    }
}
