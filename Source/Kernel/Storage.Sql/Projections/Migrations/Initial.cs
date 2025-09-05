// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Projections.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(ProjectionsDbContext))]
[Migration("Projections-Initial")]
public class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Projections",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                Owner = table.Column<int>(type: "INTEGER", nullable: false),
                ReadModelName = table.Column<string>(type: "TEXT", nullable: false),
                ReadModelGeneration = table.Column<int>(type: "INTEGER", nullable: false),
                SinkType = table.Column<Guid>(type: "TEXT", nullable: false),
                SinkConfigurationId = table.Column<Guid>(type: "TEXT", nullable: false),
                Definitions = table.JsonColumn<IDictionary<string, string>>(migrationBuilder),
            },
            constraints: table => table.PrimaryKey("PK_Projections", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Projections");
    }
}
