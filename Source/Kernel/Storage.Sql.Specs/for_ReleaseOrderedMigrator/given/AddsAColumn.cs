// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.for_ReleaseOrderedMigrator.given;

[DbContext(typeof(ReleasesDbContext))]
[Migration("Releases-v19_15_0")]
public class AddsAColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.AddColumn<string>("Name", "Releases", nullable: true);
}
