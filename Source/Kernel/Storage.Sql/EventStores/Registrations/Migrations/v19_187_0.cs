// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Registrations.Migrations;

/// <summary>
/// Adds the durable registration intents the kernel accepts before it applies them.
/// </summary>
[DbContext(typeof(EventStoreDbContext))]
[Migration("ES-RegistrationIntents-v19_187_0")]
public class v19_187_0 : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder) => migrationBuilder.CreateTable(
        name: WellKnownTableNames.RegistrationIntents,
        columns: table => new
        {
            Id = table.StringColumn(migrationBuilder, maxLength: 64, nullable: false),
            Kind = table.NumberColumn<int>(migrationBuilder, nullable: false),
            Key = table.StringColumn(migrationBuilder, nullable: false),
            Registrant = table.StringColumn(migrationBuilder, nullable: false),
            Revision = table.NumberColumn<long>(migrationBuilder, nullable: false),
            Hash = table.StringColumn(migrationBuilder, nullable: false),
            State = table.NumberColumn<int>(migrationBuilder, nullable: false),
            Payload = table.Column<byte[]>(nullable: false),
            FullSetOwner = table.NumberColumn<int>(migrationBuilder, nullable: true),
            Version = table.StringColumn(migrationBuilder, nullable: true),
            Attempts = table.NumberColumn<int>(migrationBuilder, nullable: false),
            Failure = table.StringColumn(migrationBuilder, nullable: true),
            UpdatedAt = table.DateTimeOffsetColumn(migrationBuilder)
        },
        constraints: table => table.PrimaryKey("PK_RegistrationIntents", row => row.Id));

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder) => migrationBuilder.DropTable(WellKnownTableNames.RegistrationIntents);
}
