// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicSchemaGuard;

public class when_refusing_missing_read_model_indexes : EventStores.Namespaces.ReadModels.for_ReadModelMigrator.given.a_read_model_migrator
{
    readonly List<Exception> _errors = [];
    readonly List<bool> _missingAfterRefusal = [];

    async Task Because()
    {
        var schema = await JsonSchema.FromJsonAsync("""{"type":"object","properties":{"id":{"type":"object","properties":{"part":{"type":"string"}}}}}""");
        foreach (var prefix in (string[])["chronicle_shx_", "chronicle_imx_"])
        {
            await using var context = CreateContext($"refuse_{prefix}", ProjectedColumns.ForSchema(schema));
            await context.EnsureTableExists();
            var names = await context.Database.SqlQuery<string>($"SELECT name AS Value FROM pragma_index_list({context.TableName})").ToArrayAsync();
            var migration = new MigrationBuilder(context.Database.ProviderName);
            foreach (var name in names.Where(name => name.StartsWith(prefix, StringComparison.Ordinal))) migration.DropIndex(name, context.TableName);
            await new TableMigrator<ReadModelDbContext>(Substitute.For<ILogger<TableMigrator<ReadModelDbContext>>>()).ExecuteMigrationOperations(context, migration);
            _migrator.ClearMigrationCache(context.Database.GetConnectionString()!);
            using var policy = DynamicSchemaGuard.Use(DynamicSchemaPolicy.Refuse);
            _errors.Add(await Catch.Exception(context.EnsureTableExists));
            _missingAfterRefusal.Add(prefix == "chronicle_shx_" ? await ShadowColumnIndexes.NeedsReconciliation(context) : await InitializedMarkerIndex.NeedsIndex(context));
        }
    }

    [Fact] void should_refuse_each_index_build() => _errors.ShouldEachConformTo(error => error is DynamicSchemaObjectNotProvisioned);
    [Fact] void should_not_execute_the_refused_ddl() => _missingAfterRefusal.ShouldEachConformTo(missing => missing);
}
