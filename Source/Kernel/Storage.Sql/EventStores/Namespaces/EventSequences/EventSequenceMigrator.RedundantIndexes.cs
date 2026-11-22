// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;

public partial class EventSequenceMigrator
{
    static async Task<(string Schema, string Name)?> FindRedundantSequenceIndex(EventSequenceDbContext context, string tableName)
    {
        await context.Database.OpenConnectionAsync();
        try
        {
            await using var command = context.Database.GetDbConnection().CreateCommand();

            // Names establish legacy ownership (including PostgreSQL's byte truncation), never redundancy.
            // Nondefault/custom definitions are conservatively retained, even if their name is the legacy name.
            command.CommandText = "SELECT ns.nspname, ix.relname FROM pg_class t" +
                " JOIN pg_namespace ns ON ns.oid = t.relnamespace" +
                " JOIN pg_attribute a ON a.attrelid = t.oid AND a.attname = 'SequenceNumber' AND a.attnotnull AND NOT a.attisdropped" +
                " JOIN pg_index p ON p.indrelid = t.oid AND p.indisprimary AND p.indisunique" +
                " AND p.indisvalid AND p.indisready AND p.indislive" +
                " AND p.indnkeyatts = 1 AND p.indnatts = 1 AND p.indkey[0] = a.attnum" +
                " AND p.indpred IS NULL AND p.indexprs IS NULL" +
                " JOIN pg_class pk ON pk.oid = p.indexrelid AND pk.relkind = 'i'" +
                " JOIN pg_am am ON am.oid = pk.relam AND am.amname = 'btree'" +
                " JOIN pg_index i ON i.indrelid = t.oid" +
                " AND NOT i.indisprimary AND NOT i.indisunique AND NOT i.indisexclusion" +
                " AND i.indisvalid AND i.indisready AND i.indislive AND NOT i.indisreplident AND NOT i.indisclustered" +
                " AND i.indnkeyatts = 1 AND i.indnatts = 1 AND i.indkey = p.indkey" +
                " AND i.indclass = p.indclass AND i.indcollation = p.indcollation AND i.indoption = p.indoption" +
                " AND i.indpred IS NULL AND i.indexprs IS NULL" +
                " JOIN pg_class ix ON ix.oid = i.indexrelid AND ix.relkind = 'i' AND ix.relam = pk.relam" +
                " AND ix.relnamespace = t.relnamespace AND ix.relowner = t.relowner" +
                " AND ix.reltablespace = pk.reltablespace AND ix.reloptions IS NULL AND pk.reloptions IS NULL" +
                " WHERE ns.nspname = current_schema() AND t.relname = @table AND t.relkind = 'r'" +
                " AND ix.relname = CAST(@index AS name)" +
                " AND NOT EXISTS (SELECT 1 FROM pg_constraint c WHERE c.conindid = i.indexrelid)";
            command.AddName("table", tableName);
            command.AddName("index", $"IX_{tableName}_SequenceNumber");
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;
            var index = (reader.GetString(0), reader.GetString(1));
            return await reader.ReadAsync() ? null : index;
        }
        finally
        {
            await context.Database.CloseConnectionAsync();
        }
    }

    async Task RemoveRedundantSequenceIndex(EventSequenceDbContext context, string tableName)
    {
        if (!context.Database.IsNpgsql()) return;
        var index = await FindRedundantSequenceIndex(context, tableName);
        if (index is null) return;

        // Runs under TableMigrator's existing schema lock/cache lifecycle, without CASCADE.
        var sql = context.GetService<ISqlGenerationHelper>();
        var migration = new MigrationBuilder(context.Database.ProviderName);
        migration.Sql($"DROP INDEX IF EXISTS {sql.DelimitIdentifier(index.Value.Name, index.Value.Schema)};");
        await tableMigrator.ExecuteMigrationOperations(context, migration);
    }
}
