// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Seeding.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.EventSeeds}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // NamespaceDbContext and EventStoreDbContext target separate databases per backend
        // (separate files in SQLite, separate databases in PostgreSQL/MSSQL, separate
        // databases in MongoDB), so each context needs its own EventSeeds table — earlier
        // attempts to "share" the table at the namespace level left the namespace database
        // without the table entirely, and the namespace-level EventSeedingStorage querying
        // through NamespaceDbContext.EventSeeds then failed with "no such table" the first
        // time any seeding API was invoked.
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.EventSeeds,
            columns: table => new
            {
                Id = table.NumberColumn<int>(migrationBuilder, nullable: false),
                ByEventTypeJson = table.JsonColumn<string>(migrationBuilder),
                ByEventSourceJson = table.JsonColumn<string>(migrationBuilder)
            },
            constraints: table => table.PrimaryKey($"PK_NS_{WellKnownTableNames.EventSeeds}", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: WellKnownTableNames.EventSeeds);
    }
}
