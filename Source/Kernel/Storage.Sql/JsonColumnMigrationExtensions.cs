// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Migrations.Operations.Builders;

namespace Cratis.Chronicle.Storage.Sql;

public static class JsonColumnMigrationExtensions
{
    public static OperationBuilder<AddColumnOperation> JsonColumn<TProperty>(this ColumnsBuilder cb, MigrationBuilder mb)
    {
        // if (mb.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        // {
        //     return cb.Column<TProperty>("jsonb", nullable: false);
        // }
        // else if (mb.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
        // {
        //     return cb.Column<TProperty>("nvarchar(max)", nullable: false);
        // }

        return cb.Column<TProperty>("text", nullable: false);
    }

    public static void EnsureJsonColumn(this MigrationBuilder mb, string table, string column)
    {
        if (mb.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            // native GIN index on jsonb
            mb.Sql($"CREATE INDEX ix_orders_metadata_gin ON \"{table}\" USING GIN (\"{column}\");");
        }
        else if (mb.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
        {
            mb.Sql($"ALTER TABLE [{table}] ADD CONSTRAINT CK_{table}_Metadata_IsJson CHECK(ISJSON([{column}]) = 1); ");
        }
        else if (mb.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
        {
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
            mb.Sql($"""
                CREATE TRIGGER validate_{table}_metadata_insert
                BEFORE INSERT ON {table}
                FOR EACH ROW
                WHEN json_valid(NEW.{column}) = 0
                BEGIN
                    SELECT RAISE(ABORT, 'Metadata must be valid JSON');
                END;
                """);
#pragma warning restore MA0136 // Raw String contains an implicit end of line character
        }
    }
}

