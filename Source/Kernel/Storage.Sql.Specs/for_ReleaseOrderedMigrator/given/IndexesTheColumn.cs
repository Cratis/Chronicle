// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.for_ReleaseOrderedMigrator.given;

[DbContext(typeof(ReleasesDbContext))]
[Migration("Releases-v19_104_0")]
public class IndexesTheColumn : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateIndex("IX_Releases_Name", "Releases", "Name");
}
