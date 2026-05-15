// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(ClusterDbContext))]
[Migration($"Cluster-{nameof(v15_1_0)}")]
public class v15_1_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Users,
            columns: table => new
            {
                Id = table.GuidColumn(migrationBuilder, nullable: false),
                Username = table.StringColumn(migrationBuilder),
                Email = table.StringColumn(migrationBuilder, nullable: true),
                PasswordHash = table.StringColumn(migrationBuilder, nullable: true),
                SecurityStamp = table.StringColumn(migrationBuilder, nullable: true),
                IsActive = table.BoolColumn(migrationBuilder),
                CreatedAt = table.DateTimeOffsetColumn(migrationBuilder),
                LastModifiedAt = table.DateTimeOffsetColumn(migrationBuilder, nullable: true),
                RequiresPasswordChange = table.BoolColumn(migrationBuilder),
                HasLoggedIn = table.BoolColumn(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Users}", x => x.Id));

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Applications,
            columns: table => new
            {
                Id = table.GuidColumn(migrationBuilder, nullable: false),
                ClientId = table.StringColumn(migrationBuilder),
                ClientSecret = table.StringColumn(migrationBuilder, nullable: true),
                DisplayName = table.StringColumn(migrationBuilder, nullable: true),
                Type = table.StringColumn(migrationBuilder, nullable: true),
                ConsentType = table.StringColumn(migrationBuilder, nullable: true),
                Permissions = table.JsonColumn<IEnumerable<string>>(migrationBuilder),
                Requirements = table.JsonColumn<IEnumerable<string>>(migrationBuilder),
                RedirectUris = table.JsonColumn<IEnumerable<string>>(migrationBuilder),
                PostLogoutRedirectUris = table.JsonColumn<IEnumerable<string>>(migrationBuilder),
                Properties = table.StringColumn(migrationBuilder, nullable: true),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Applications}", x => x.Id));

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.DataProtectionKeys,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                FriendlyName = table.StringColumn(migrationBuilder),
                Xml = table.StringColumn(migrationBuilder),
                Created = table.DateTimeOffsetColumn(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.DataProtectionKeys}", x => x.Id));

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Patches,
            columns: table => new
            {
                Name = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Version = table.StringColumn(migrationBuilder),
                AppliedAt = table.DateTimeOffsetColumn(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Patches}", x => x.Name));

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.SystemInformation,
            columns: table => new
            {
                Id = table.NumberColumn<int>(migrationBuilder),
                Version = table.StringColumn(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.SystemInformation}", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: WellKnownTableNames.Users);
        migrationBuilder.DropTable(name: WellKnownTableNames.Applications);
        migrationBuilder.DropTable(name: WellKnownTableNames.DataProtectionKeys);
        migrationBuilder.DropTable(name: WellKnownTableNames.Patches);
        migrationBuilder.DropTable(name: WellKnownTableNames.SystemInformation);
    }
}
