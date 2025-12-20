// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Changesets.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Changesets}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Changesets,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder),
                ReadModelName = table.StringColumn(migrationBuilder),
                ReadModelKey = table.StringColumn(migrationBuilder),
                EventType = table.StringColumn(migrationBuilder),
                SequenceNumber = table.NumberColumn<ulong>(migrationBuilder),
                CorrelationId = table.GuidColumn(migrationBuilder),
                ChangesetData = table.JsonColumn<string>(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Changesets}", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: WellKnownTableNames.Changesets);
    }
}
