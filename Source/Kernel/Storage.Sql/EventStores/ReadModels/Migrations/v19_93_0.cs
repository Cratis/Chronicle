// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.ReadModels.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"ES-{WellKnownTableNames.ReadModelDefinitions}-{nameof(v19_93_0)}")]
public class v19_93_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<string>(
        name: nameof(ReadModelDefinition.IndexesJson), table: WellKnownTableNames.ReadModelDefinitions, nullable: false, defaultValue: "[]");

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: nameof(ReadModelDefinition.IndexesJson), table: WellKnownTableNames.ReadModelDefinitions);
}
