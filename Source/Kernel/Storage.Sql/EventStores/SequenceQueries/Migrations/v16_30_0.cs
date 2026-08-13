// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.SequenceQueries.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.SequenceQueries}-{nameof(v16_30_0)}")]
public class v16_30_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.SequenceQueries,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Name = table.StringColumn(migrationBuilder),
                Scope = table.NumberColumn<int>(migrationBuilder),
                Owner = table.StringColumn(migrationBuilder),
                Namespace = table.StringColumn(migrationBuilder),
                EventSequenceId = table.StringColumn(migrationBuilder),
                Filter = table.JsonColumn<SequenceQueryFilter>(migrationBuilder),
                Descending = table.Column<bool>(nullable: false),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.SequenceQueries}", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.SequenceQueries);
    }
}
