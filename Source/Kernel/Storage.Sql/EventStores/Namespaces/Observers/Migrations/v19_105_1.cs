// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Json;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers.Migrations;

#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(NamespaceDbContext))]
[Migration($"NS-{WellKnownTableNames.Observers}-{nameof(v19_105_1)}")]
public class v19_105_1 : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            migrationBuilder.AddColumn<string>(name: "WebhookRetirement", table: WellKnownTableNames.Observers, type: "jsonb", nullable: true);
        }
        else
        {
            migrationBuilder.AddJsonColumn<string>(name: "WebhookRetirement", table: WellKnownTableNames.Observers);
            migrationBuilder.Operations.OfType<AddColumnOperation>().Single(column => column.Name == "WebhookRetirement").IsNullable = true;
        }

        migrationBuilder.AddColumn<Guid>(name: "WebhookRegistrationId", table: WellKnownTableNames.Observers, nullable: true);
        migrationBuilder.AddColumn<string>(name: "WebhookRegistrationFingerprint", table: WellKnownTableNames.Observers, nullable: true);
        migrationBuilder.AddColumn<bool>(name: "WebhookReplayRetained", table: WellKnownTableNames.Observers, nullable: false, defaultValue: false);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        if (ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer")
        {
            migrationBuilder.DropCheckConstraint(name: "CK_Observers_WebhookRetirement_IsJson", table: WellKnownTableNames.Observers);
        }

        migrationBuilder.DropColumn(name: "WebhookRetirement", table: WellKnownTableNames.Observers);
        migrationBuilder.DropColumn(name: "WebhookRegistrationId", table: WellKnownTableNames.Observers);
        migrationBuilder.DropColumn(name: "WebhookRegistrationFingerprint", table: WellKnownTableNames.Observers);
        migrationBuilder.DropColumn(name: "WebhookReplayRetained", table: WellKnownTableNames.Observers);
    }
}
