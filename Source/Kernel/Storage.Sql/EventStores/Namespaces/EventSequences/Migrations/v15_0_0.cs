// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.EventSequences}-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create EventSequences table for storing event sequence state
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.EventSequences,
            columns: table => new
            {
                EventSequenceId = table.StringColumn(migrationBuilder),
                SequenceNumber = table.Column<decimal>(nullable: false),
                TailSequenceNumberPerEventType = table.JsonColumn<IDictionary<string, object>>(migrationBuilder)
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.EventSequences}", x => x.EventSequenceId));
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop only the EventSequences state table
        // Individual event sequence tables are managed by EventSequenceMigrator
        migrationBuilder.DropTable(name: WellKnownTableNames.EventSequences);
    }
}
