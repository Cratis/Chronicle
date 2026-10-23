// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ShadowColumnIndexes;

public class when_an_existing_shadow_index_is_invalid : for_ReadModelMigrator.given.a_read_model_migrator
{
    readonly Commands _commands = new();
    ReadModelDbContext _context;
    string[] _indexes;
    int? _restoredTimeout;

    async Task Establish()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"part":{"type":"string"}}}}}""");
        var columns = ProjectedColumns.ForSchema(schema);
        await using var old = CreateContext("invalid_shadow", columns.Select(column => column with { IsCompositeKey = false }).ToArray());
        await old.EnsureTableExists();
        await old.Database.ExecuteSqlRawAsync("ALTER TABLE invalid_shadow ADD COLUMN __compositeKey TEXT; CREATE INDEX chronicle_shx_invalid ON invalid_shadow(__compositeKey)");
        var options = new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite(old.Database.GetConnectionString()).AddInterceptors(_commands).Options;
        _context = new ReadModelDbContext(options, old.TableName, columns, _migrator);
        _context.Database.SetCommandTimeout(19);
    }

    async Task Because()
    {
        await ShadowColumnIndexes.Ensure(_context);
        await ShadowColumnIndexes.Ensure(_context);
        _restoredTimeout = _context.Database.GetCommandTimeout();
        _indexes = await _context.Database.SqlQuery<string>($"SELECT name AS Value FROM pragma_index_list('invalid_shadow') WHERE name LIKE 'chronicle_shx_%'").ToArrayAsync();
    }

    void Destroy() => _context.Dispose();

    [Fact] void should_drop_the_invalid_index() => _commands.Sql.Exists(sql => sql == "DROP INDEX IF EXISTS \"chronicle_shx_invalid\"").ShouldBeTrue();
    [Fact] void should_rebuild_the_column_index_only_once() => _commands.Sql.Count(sql => sql.StartsWith("CREATE INDEX", StringComparison.Ordinal)).ShouldEqual(1);
    [Fact] void should_not_adopt_the_invalid_index_name() => _indexes.ShouldNotContain("chronicle_shx_invalid");
    [Fact] void should_leave_one_usable_shadow_index() => _indexes.Length.ShouldEqual(1);
    [Fact] void should_use_the_dedicated_timeout_for_both_drop_and_create() => _commands.DdlTimeouts.ShouldEachConformTo(timeout => timeout == 0);
    [Fact] void should_restore_a_custom_caller_timeout() => _restoredTimeout.ShouldEqual(19);

    sealed class Commands : DbCommandInterceptor
    {
        internal List<string> Sql { get; } = [];
        internal List<int> DdlTimeouts { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("pragma_index_info", StringComparison.Ordinal))
            {
                // SQLite has no invalid-index state. Supply PostgreSQL's catalog state through the real reconciliation query.
                command.CommandText = "SELECT i.name AS Name, c.name AS \"Column\", i.name <> 'chronicle_shx_invalid' AS IsValid FROM pragma_index_list('invalid_shadow') i JOIN pragma_index_info(i.name) c WHERE i.\"unique\" = 0 AND substr(i.name, 1, 14) = 'chronicle_shx_'";
            }
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Sql.Add(command.CommandText);
            DdlTimeouts.Add(command.CommandTimeout);
            return ValueTask.FromResult(result);
        }
    }
}
