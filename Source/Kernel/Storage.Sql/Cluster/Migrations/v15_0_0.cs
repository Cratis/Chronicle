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
[Migration($"Cluster-{nameof(v15_0_0)}")]
public class v15_0_0 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: WellKnownTableNames.EventStores,
            columns: table => new
            {
                Name = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.EventStores}", x => x.Name));

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Reminders,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                GrainId = table.StringColumn(migrationBuilder, nullable: false),
                GrainHash = table.NumberColumn<uint>(migrationBuilder, nullable: false),
                ReminderName = table.StringColumn(migrationBuilder, nullable: false),
                Period = table.NumberColumn<long>(migrationBuilder, nullable: false),
                StartAt = table.NumberColumn<long>(migrationBuilder, nullable: false),
                ETag = table.StringColumn(migrationBuilder, nullable: false),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Reminders}", x => x.Id));

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
                Id = table.NumberColumn<int>(migrationBuilder, nullable: false),
                Version = table.StringColumn(migrationBuilder),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.SystemInformation}", x => x.Id));

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Tokens,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                ApplicationId = table.StringColumn(migrationBuilder, nullable: true),
                AuthorizationId = table.StringColumn(migrationBuilder, nullable: true),
                Subject = table.StringColumn(migrationBuilder, nullable: true),
                Type = table.StringColumn(migrationBuilder, nullable: true),
                Status = table.StringColumn(migrationBuilder, nullable: true),
                Payload = table.StringColumn(migrationBuilder, nullable: true),
                ReferenceId = table.StringColumn(migrationBuilder, nullable: true),
                CreationDate = table.DateTimeOffsetColumn(migrationBuilder, nullable: true),
                ExpirationDate = table.DateTimeOffsetColumn(migrationBuilder, nullable: true),
                RedemptionDate = table.DateTimeOffsetColumn(migrationBuilder, nullable: true),
                Properties = table.StringColumn(migrationBuilder, nullable: true),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Tokens}", x => x.Id));

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Authorizations,
            columns: table => new
            {
                Id = table.GuidColumn(migrationBuilder, nullable: false),
                ApplicationId = table.GuidColumn(migrationBuilder, nullable: true),
                Subject = table.StringColumn(migrationBuilder, nullable: true),
                Type = table.StringColumn(migrationBuilder, nullable: true),
                Status = table.StringColumn(migrationBuilder, nullable: true),
                Scopes = table.StringColumn(migrationBuilder, nullable: true),
                CreationDate = table.DateTimeOffsetColumn(migrationBuilder, nullable: true),
                Properties = table.StringColumn(migrationBuilder, nullable: true),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Authorizations}", x => x.Id));

        migrationBuilder.CreateTable(
            name: WellKnownTableNames.Scopes,
            columns: table => new
            {
                Id = table.StringColumn(migrationBuilder, maxLength: 200, nullable: false),
                Name = table.StringColumn(migrationBuilder, nullable: true),
                DisplayName = table.StringColumn(migrationBuilder, nullable: true),
                Description = table.StringColumn(migrationBuilder, nullable: true),
                Resources = table.StringColumn(migrationBuilder, nullable: true),
                Properties = table.StringColumn(migrationBuilder, nullable: true),
            },
            constraints: table => table.PrimaryKey($"PK_{WellKnownTableNames.Scopes}", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: WellKnownTableNames.EventStores);
        migrationBuilder.DropTable(name: WellKnownTableNames.Reminders);
        migrationBuilder.DropTable(name: WellKnownTableNames.Users);
        migrationBuilder.DropTable(name: WellKnownTableNames.Applications);
        migrationBuilder.DropTable(name: WellKnownTableNames.DataProtectionKeys);
        migrationBuilder.DropTable(name: WellKnownTableNames.Patches);
        migrationBuilder.DropTable(name: WellKnownTableNames.SystemInformation);
        migrationBuilder.DropTable(name: WellKnownTableNames.Tokens);
        migrationBuilder.DropTable(name: WellKnownTableNames.Authorizations);
        migrationBuilder.DropTable(name: WellKnownTableNames.Scopes);
    }
}
