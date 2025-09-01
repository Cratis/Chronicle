// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.EventTypes.Migrations;

#nullable disable
#pragma warning disable SA1600, SA1402, MA0048

[DbContext(typeof(EventTypesDbContext))]
[Migration("EventTypes-Initial")]
public class Initial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "EventTypes",
            columns: table => new
            {
                Id = table.Column<string>(type: "TEXT", nullable: false),
                Owner = table.Column<int>(type: "INTEGER", nullable: false),
                Tombstone = table.Column<bool>(type: "INTEGER", nullable: false),
                Schemas = table.Column<IDictionary<string, string>>(type: "TEXT", nullable: false),
            },
            constraints: table => table.PrimaryKey("PK_EventTypes", x => x.Id));
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "EventTypes");
    }
}

[DbContext(typeof(EventTypesDbContext))]
public class InitialSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder) => modelBuilder
        .Entity(typeof(EventType).FullName, b =>
        {
            b.Property<string>(nameof(EventType.Id))
                .HasColumnType("TEXT")
                .IsRequired();

            b.Property<EventTypeOwner>(nameof(EventType.Owner))
                .HasColumnType("INTEGER")
                .IsRequired();

            b.Property<bool>(nameof(EventType.Tombstone))
                .HasColumnType("INTEGER")
                .IsRequired();

            b.Property<IDictionary<string, string>>(nameof(EventType.Schemas))
                .HasColumnType("TEXT")
                .IsRequired();

            b.HasKey(nameof(EventType.Id));
            b.ToTable("EventTypes");
        });
}
