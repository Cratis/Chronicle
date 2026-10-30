// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Schemas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels.for_ShadowColumnIndexes;

public class when_the_shadow_ddl_timeout_is_configured : for_ReadModelMigrator.given.a_read_model_migrator
{
    readonly Commands _commands = new();
    int? _restoredTimeout;
    int _configuredTimeout;

    async Task Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Sql:ShadowIndexDdlTimeoutSeconds"] = "73" }).Build().Get<ChronicleOptions>();
        _configuredTimeout = configuration.Sql.ShadowIndexDdlTimeoutSeconds;
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"part":{"type":"string"}}}}}""");
        var columns = ProjectedColumns.ForSchema(schema);
        await using var plain = CreateContext("configured_timeout", columns.Select(column => column with { IsCompositeKey = false }).ToArray());
        await plain.EnsureTableExists();
        await plain.Database.ExecuteSqlRawAsync("ALTER TABLE configured_timeout ADD COLUMN __compositeKey TEXT; CREATE INDEX chronicle_shx_obsolete ON configured_timeout(id)");
        var options = new DbContextOptionsBuilder<ReadModelDbContext>().UseSqlite(plain.Database.GetConnectionString()).AddInterceptors(_commands).Options;
        var migrator = new ReadModelMigrator(new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()), Substitute.For<ILogger<ReadModelMigrator>>(), Options.Create(configuration));
        migrator.ClearMigrationCache(plain.Database.GetConnectionString());
        await using var context = new ReadModelDbContext(options, plain.TableName, columns, migrator);
        context.Database.SetCommandTimeout(19);
        await context.EnsureTableExists();
        _restoredTimeout = context.Database.GetCommandTimeout();
    }

    [Fact] void should_bind_the_existing_sql_configuration_section() => _configuredTimeout.ShouldEqual(73);
    [Fact] void should_default_to_ten_minutes() => new Configuration.Sql().ShadowIndexDdlTimeoutSeconds.ShouldEqual(600);
    [Fact] void should_use_the_override_for_create_and_drop() => _commands.DdlTimeouts.ShouldEachConformTo(timeout => timeout == 73);
    [Fact] void should_run_both_shadow_ddl_statements() => _commands.DdlTimeouts.Count.ShouldEqual(2);
    [Fact] void should_keep_catalog_queries_on_the_callers_timeout() => _commands.QueryTimeouts.ShouldEachConformTo(timeout => timeout == 19);
    [Fact] void should_restore_the_callers_timeout() => _restoredTimeout.ShouldEqual(19);

    sealed class Commands : DbCommandInterceptor
    {
        internal List<int> DdlTimeouts { get; } = [];
        internal List<int> QueryTimeouts { get; } = [];

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            DdlTimeouts.Add(command.CommandTimeout);
            return ValueTask.FromResult(result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        {
            QueryTimeouts.Add(command.CommandTimeout);
            return ValueTask.FromResult(result);
        }
    }
}
