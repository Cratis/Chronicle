// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.Migrations;

#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Observers}-{nameof(v19_79_0)}")]
public class v19_79_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var name in new[] { "TypeSetControl", "Initialization" })
        {
            if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                // Native jsonb validates the value itself. Arc's AddJsonColumn generator joins
                // its auxiliary CHECK and CREATE INDEX without a statement separator.
                migrationBuilder.AddColumn<string>(
                    name: name,
                    table: WellKnownTableNames.Observers,
                    type: "jsonb",
                    nullable: true);
            }
            else
            {
                migrationBuilder.AddJsonColumn<string>(name: name, table: WellKnownTableNames.Observers);
                migrationBuilder.Operations.OfType<AddColumnOperation>().Single(column => column.Name == name).IsNullable = true;
            }
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "TypeSetControl", table: WellKnownTableNames.Observers);
        migrationBuilder.DropColumn(name: "Initialization", table: WellKnownTableNames.Observers);
    }
}
