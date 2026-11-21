// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using JobsDbContext = Cratis.Choreography.Storage.Sql.Jobs.JobsDbContext;

namespace Cratis.Chronicle.Storage.Sql;

public partial class Database
{
    /// <inheritdoc/>
    public async Task<bool> NamespaceHasEvents(EventStoreName eventStore, EventStoreNamespaceName @namespace)
    {
        var target = await ExistingTarget(new(DatabaseKind.Namespace, eventStore.Value, @namespace.Value), ConnectionPurpose.Runtime);
        if (target is null)
        {
            return false;
        }

        var schema = string.IsNullOrEmpty(target.Schema) ? null : target.Schema;
        await using var context = new NamespaceDbContext(ReadOptions<NamespaceDbContext>(target));
        IReadOnlyList<string> tables;
        try
        {
            if (!await context.GetService<IRelationalDatabaseCreator>().ExistsAsync())
            {
                return false;
            }

            tables = await ExistingNamespaceTables(context, schema);
        }
        catch (Exception exception) when (IsMissingStorage(exception))
        {
            return false;
        }

        await using var jobs = new JobsDbContext(ReadOptions<JobsDbContext>(target));

        // Exclude only known operational state. Unknown or auxiliary tables with rows conservatively
        // count as event history rather than risk hiding a custom sequence or an untracked event table.
        var operationalTables = context.Model.GetEntityTypes().Concat(jobs.Model.GetEntityTypes())
            .Select(_ => _.GetTableName()).ToHashSet(StringComparer.Ordinal);
        operationalTables.Add("__EFMigrationsHistory");
        foreach (var table in tables.Where(table => !operationalTables.Contains(table)))
        {
            if (await TableHasRows(context, table, schema))
            {
                return true;
            }
        }

        return false;
    }

    static async Task<IReadOnlyList<string>> ExistingNamespaceTables(DbContext context, string? schema)
    {
        await context.Database.OpenConnectionAsync();
        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText = "SELECT table_name FROM information_schema.tables WHERE table_type = 'BASE TABLE' AND table_schema = COALESCE(@schema, current_schema())";
            if (context.Database.IsSqlite())
            {
                command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%'";
            }
            else if (context.Database.IsSqlServer())
            {
                command.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_SCHEMA = COALESCE(@schema, SCHEMA_NAME())";
            }

            var parameter = command.CreateParameter();
            parameter.ParameterName = "@schema";
            parameter.Value = (object?)schema ?? DBNull.Value;
            parameter.DbType = System.Data.DbType.String;
            command.Parameters.Add(parameter);
            await using var reader = await command.ExecuteReaderAsync();
            var tables = new List<string>();
            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }

            return tables;
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    static async Task<bool> TableHasRows(DbContext context, string table, string? schema)
    {
        var identifier = context.GetService<ISqlGenerationHelper>().DelimitIdentifier(table, schema);
        var query = context.Database.IsSqlServer()
            ? $"SELECT TOP(1) 1 AS Value FROM {identifier}"
            : $"SELECT 1 AS Value FROM {identifier} LIMIT 1";
        return (await context.Database.SqlQueryRaw<int>(query).ToListAsync()).Count > 0;
    }
}
