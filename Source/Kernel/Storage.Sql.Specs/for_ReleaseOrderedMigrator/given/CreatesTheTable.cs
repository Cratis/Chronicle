// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.for_ReleaseOrderedMigrator.given;

[DbContext(typeof(ReleasesDbContext))]
[Migration("Releases-v19_9_0")]
public class CreatesTheTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateTable("Releases", table => new { Id = table.Column<int>() }, constraints: table => table.PrimaryKey("PK_Releases", _ => _.Id));
}
