// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.Migrations;

#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Observers}-{nameof(v19_15_0)}")]
public class v19_15_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "AlertLifecycleId", table: WellKnownTableNames.Observers, nullable: false, defaultValue: Guid.Empty);
        migrationBuilder.AddColumn<long>(name: "AlertRevision", table: WellKnownTableNames.Observers, nullable: false, defaultValue: 0L);
        migrationBuilder.AddColumn<int>(name: "AlertDisposition", table: WellKnownTableNames.Observers, nullable: false, defaultValue: 0);
        migrationBuilder.AddColumn<Guid>(name: "QuarantineEpisodeId", table: WellKnownTableNames.Observers, nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "AlertLifecycleId", table: WellKnownTableNames.Observers);
        migrationBuilder.DropColumn(name: "AlertRevision", table: WellKnownTableNames.Observers);
        migrationBuilder.DropColumn(name: "AlertDisposition", table: WellKnownTableNames.Observers);
        migrationBuilder.DropColumn(name: "QuarantineEpisodeId", table: WellKnownTableNames.Observers);
    }
}
