// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        // Event seeding state is shared with EventStoreDbContext and created by
        // Cratis.Chronicle.Storage.Sql.EventStores.Seeding.Migrations.v15_0_0.
        // Creating the same table from the namespace context causes provider-specific
        // "table already exists" failures (notably PostgreSQL) during startup.
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Shared table - dropped by EventStoreDbContext migration down path.
    }
}
