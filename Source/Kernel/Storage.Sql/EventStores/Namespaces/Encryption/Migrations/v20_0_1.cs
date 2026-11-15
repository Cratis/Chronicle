// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Encryption.Migrations;

#pragma warning disable SA1600, SA1402, MA0048

/// <summary>
/// Makes the identifier columns of the encryption key tables compare exactly on SQL Server and widens them to 256 characters.
/// </summary>
/// <remarks>
/// Nothing is merged, dropped or re-keyed. Every spelling that was stored before the upgrade is recorded in
/// <see cref="WellKnownTableNames.EncryptionKeyIdentifierReviews"/> together with its pre-upgrade collation.
/// </remarks>
[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.EncryptionKeys}-{nameof(v20_0_1)}")]
public class v20_0_1 : Migration
{
    const string Exact = "Latin1_General_100_BIN2";
    const string EndsWithTerminator = "UNICODE(RIGHT([Identifier], 1)) = 1";
    const string EndsWithSpaceOrTerminator = "UNICODE(RIGHT([Identifier], 1)) IN (32, 1)";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider!.Contains("SqlServer", StringComparison.Ordinal))
        {
            UpOnSqlServer(migrationBuilder);
        }
        else if (ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
        {
            Resize(migrationBuilder, 256);
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider!.Contains("SqlServer", StringComparison.Ordinal))
        {
            DownOnSqlServer(migrationBuilder);
        }
        else if (ActiveProvider.Contains("Npgsql", StringComparison.Ordinal))
        {
            Resize(migrationBuilder, 200);
        }
    }

    static void Resize(MigrationBuilder migrationBuilder, int length)
    {
        migrationBuilder.Sql($"ALTER TABLE \"{WellKnownTableNames.EncryptionKeys}\" ALTER COLUMN \"Identifier\" TYPE character varying({length})");
        migrationBuilder.Sql($"ALTER TABLE \"{WellKnownTableNames.EncryptionKeyErasures}\" ALTER COLUMN \"Identifier\" TYPE character varying({length})");
    }

    static void UpOnSqlServer(MigrationBuilder migrationBuilder)
    {
        // The collation name is concatenated into DDL, so it is read from the catalog and must be a known collation.
        migrationBuilder.Sql(string.Join(
            ' ',
            $"DECLARE @keys sysname = (SELECT [collation_name] FROM sys.columns WHERE [object_id] = OBJECT_ID(N'[{WellKnownTableNames.EncryptionKeys}]') AND [name] = N'Identifier');",
            $"DECLARE @erasures sysname = (SELECT [collation_name] FROM sys.columns WHERE [object_id] = OBJECT_ID(N'[{WellKnownTableNames.EncryptionKeyErasures}]') AND [name] = N'Identifier');",
            "IF @keys IS NULL OR @erasures IS NULL OR @keys <> @erasures OR NOT EXISTS (SELECT 1 FROM sys.fn_helpcollations() WHERE [name] = @keys)",
            "THROW 50000, N'The identifier columns of the encryption key tables do not share a known collation.', 1;",
            $"DECLARE @create nvarchar(max) = N'CREATE TABLE [{WellKnownTableNames.EncryptionKeyIdentifierReviews}] ([Identifier] nvarchar(256) COLLATE {Exact} NOT NULL CONSTRAINT [PK_{WellKnownTableNames.EncryptionKeyIdentifierReviews}] PRIMARY KEY, [LegacyIdentifier] nvarchar(256) COLLATE ' + @keys + N' NOT NULL); CREATE INDEX [IX_{WellKnownTableNames.EncryptionKeyIdentifierReviews}_LegacyIdentifier] ON [{WellKnownTableNames.EncryptionKeyIdentifierReviews}] ([LegacyIdentifier])';",
            "EXEC sys.sp_executesql @create;"));

        migrationBuilder.Sql(string.Join(
            ' ',
            $";WITH [Spellings] AS (SELECT [Identifier] FROM [{WellKnownTableNames.EncryptionKeys}] UNION ALL SELECT [Identifier] FROM [{WellKnownTableNames.EncryptionKeyErasures}]),",
            $"[Encoded] AS (SELECT [Identifier] AS [Raw], CAST([Identifier] COLLATE {Exact} AS nvarchar(256)) + CASE WHEN {EndsWithSpaceOrTerminator} THEN NCHAR(1) ELSE N'' END AS [Value] FROM [Spellings])",
            $"INSERT INTO [{WellKnownTableNames.EncryptionKeyIdentifierReviews}] ([Identifier], [LegacyIdentifier]) SELECT [Value], MIN([Raw]) FROM [Encoded] GROUP BY [Value];"));

        foreach (var table in new[] { WellKnownTableNames.EncryptionKeys, WellKnownTableNames.EncryptionKeyErasures })
        {
            migrationBuilder.Sql($"ALTER TABLE [{table}] DROP CONSTRAINT [PK_{table}]");
            migrationBuilder.Sql($"ALTER TABLE [{table}] ALTER COLUMN [Identifier] nvarchar(256) COLLATE {Exact} NOT NULL");
            migrationBuilder.Sql($"UPDATE [{table}] SET [Identifier] = [Identifier] + NCHAR(1) WHERE {EndsWithSpaceOrTerminator}");
            migrationBuilder.Sql($"ALTER TABLE [{table}] ADD CONSTRAINT [PK_{table}] PRIMARY KEY ({PrimaryKeyColumns(table)})");
        }
    }

    static void DownOnSqlServer(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { WellKnownTableNames.EncryptionKeys, WellKnownTableNames.EncryptionKeyErasures })
        {
            migrationBuilder.Sql($"ALTER TABLE [{table}] DROP CONSTRAINT [PK_{table}]");
            migrationBuilder.Sql($"UPDATE [{table}] SET [Identifier] = LEFT([Identifier], DATALENGTH([Identifier]) / 2 - 1) WHERE {EndsWithTerminator}");
            migrationBuilder.Sql($"ALTER TABLE [{table}] ALTER COLUMN [Identifier] nvarchar(200) COLLATE DATABASE_DEFAULT NOT NULL");
            migrationBuilder.Sql($"ALTER TABLE [{table}] ADD CONSTRAINT [PK_{table}] PRIMARY KEY ({PrimaryKeyColumns(table)})");
        }

        migrationBuilder.Sql($"DROP TABLE [{WellKnownTableNames.EncryptionKeyIdentifierReviews}]");
    }

    static string PrimaryKeyColumns(string table) => table == WellKnownTableNames.EncryptionKeys ? "[Identifier], [Revision]" : "[Identifier]";
}
