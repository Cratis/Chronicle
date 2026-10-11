// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Observers.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventStoreDbContext))]
[Migration($"{WellKnownTableNames.ObserverDefinitions}-{nameof(v19_42_0)}")]
public class v19_42_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.AddColumn<int>(
        name: nameof(ObserverDefinition.GenerationDelivery), table: WellKnownTableNames.ObserverDefinitions, nullable: false, defaultValue: 0);

    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropColumn(
        name: nameof(ObserverDefinition.GenerationDelivery), table: WellKnownTableNames.ObserverDefinitions);
}
